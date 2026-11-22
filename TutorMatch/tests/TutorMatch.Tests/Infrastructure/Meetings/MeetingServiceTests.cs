using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Domain.Students;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Infrastructure.Meetings;
using TutorMatch.Api.Infrastructure.Sessions;
using TutorMatch.Api.Infrastructure.Students;
using TutorMatch.Api.Infrastructure.Tutors;
using Xunit;

namespace TutorMatch.Tests.Infrastructure.Meetings;

public class MeetingServiceTests
{
    private static readonly Guid SessionId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly DateTime Start = new(2026, 12, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = Start.AddHours(1);

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;
        public List<(HttpRequestMessage Request, string Body)> Requests { get; } = new();

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((request, body));
            return _respond(request);
        }
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;
        public StubHttpClientFactory(HttpClient client) => _client = client;
        public HttpClient CreateClient(string name) => _client;
    }

    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static (GoogleMeetService Service, StubHandler Handler) CreateGoogleService(
        Func<HttpRequestMessage, HttpResponseMessage> respond,
        bool withSavedSession = true)
    {
        var handler = new StubHandler(respond);
        var options = Options.Create(new GoogleMeetOptions { ClientId = "client", ClientSecret = "secret", RefreshToken = "refresh" });

        var sessions = new InMemorySessionRepository();
        var tutors = new InMemoryTutorRepository();
        var students = new InMemoryStudentRepository();
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        tutors.Add(new Tutor("Tutor Meet", "tutor.meet@gmail.com", "Bio", 50, tutorId));
        students.Add(new Student(studentId, "Student Meet", "student.meet@gmail.com"));
        if (withSavedSession)
        {
            sessions.Add(new Session(SessionId, tutorId, studentId, Start, End));
        }

        var service = new GoogleMeetService(new StubHttpClientFactory(new HttpClient(handler)), options, sessions, tutors, students);
        return (service, handler);
    }

    private const string MeetLink = "https://meet.google.com/abc-defg-hij";

    private static HttpResponseMessage GoogleResponds(HttpRequestMessage request, string spaceJson) =>
        request.RequestUri!.Host switch
        {
            "oauth2.googleapis.com" => Json("""{"access_token":"token-123","expires_in":3599}"""),
            "meet.googleapis.com" => Json(spaceJson),
            _ => Json("""{"id":"evt1"}""")
        };

    private const string OpenSpaceJson =
        """{"name":"spaces/abc","meetingUri":"https://meet.google.com/abc-defg-hij","meetingCode":"abc-defg-hij","config":{"accessType":"OPEN"}}""";

    [Fact]
    public async Task Given_googleCreatesSpace_When_generatingLink_Then_returnsOpenSpaceLinkAndInvitesTutorAndStudent()
    {
        var (service, handler) = CreateGoogleService(request => GoogleResponds(request, OpenSpaceJson));

        var link = await service.GenerateMeetingLinkAsync(SessionId, "TutorMatch: Algebra", Start, End);

        link.Should().Be(MeetLink);
        handler.Requests.Should().HaveCount(3);
        handler.Requests[0].Body.Should().Contain("grant_type=refresh_token").And.Contain("refresh_token=refresh");

        var (spaceRequest, spaceBody) = handler.Requests[1];
        spaceRequest.RequestUri!.ToString().Should().Be("https://meet.googleapis.com/v2/spaces");
        spaceRequest.Headers.Authorization!.ToString().Should().Be("Bearer token-123");
        spaceBody.Should().Be("""{"config":{"accessType":"OPEN"}}""");

        var (eventRequest, eventBody) = handler.Requests[2];
        eventRequest.RequestUri!.ToString().Should().Contain("/calendars/primary/events").And.Contain("sendUpdates=all");
        eventRequest.Headers.Authorization!.ToString().Should().Be("Bearer token-123");
        eventBody.Should().Contain("TutorMatch: Algebra").And.Contain($"\"location\":\"{MeetLink}\"").And.NotContain("conferenceData");
        eventBody.Should().Contain("\"attendees\":[{\"email\":\"tutor.meet@gmail.com\"},{\"email\":\"student.meet@gmail.com\"}]");
    }

    [Fact]
    public async Task Given_sessionNotFound_When_generatingLink_Then_createsEventWithoutAttendees()
    {
        var (service, handler) = CreateGoogleService(request => GoogleResponds(request, OpenSpaceJson), withSavedSession: false);

        var link = await service.GenerateMeetingLinkAsync(SessionId, "Topic", Start, End);

        link.Should().Be(MeetLink);
        handler.Requests[2].Body.Should().Contain("\"attendees\":[]");
    }

    [Fact]
    public async Task Given_spaceWithoutMeetingUri_When_generatingLink_Then_throwsWithoutCreatingEvent()
    {
        var (service, handler) = CreateGoogleService(request => GoogleResponds(request, """{"name":"spaces/abc"}"""));

        var action = () => service.GenerateMeetingLinkAsync(SessionId, "Topic", Start, End);

        await action.Should().ThrowAsync<InvalidOperationException>();
        handler.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task Given_meetApiRejectsRequest_When_generatingLink_Then_throwsHttpRequestException()
    {
        var (service, _) = CreateGoogleService(request => request.RequestUri!.Host == "meet.googleapis.com"
            ? Json("""{"error":{"code":403,"status":"PERMISSION_DENIED"}}""", HttpStatusCode.Forbidden)
            : GoogleResponds(request, OpenSpaceJson));

        var action = () => service.GenerateMeetingLinkAsync(SessionId, "Topic", Start, End);

        await action.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task Given_revokedRefreshToken_When_generatingLink_Then_throwsHttpRequestExceptionWithoutCreatingEvent()
    {
        var (service, handler) = CreateGoogleService(_ => Json("""{"error":"invalid_grant"}""", HttpStatusCode.BadRequest));

        var action = () => service.GenerateMeetingLinkAsync(SessionId, "Topic", Start, End);

        await action.Should().ThrowAsync<HttpRequestException>();
        handler.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_sessionId_When_generatingJitsiLink_Then_returnsUnguessableRoomForThatSession()
    {
        var service = new JitsiMeetingService();

        var first = await service.GenerateMeetingLinkAsync(SessionId, "Topic", Start, End);
        var second = await service.GenerateMeetingLinkAsync(SessionId, "Topic", Start, End);

        first.Should().MatchRegex($"^https://meet\\.jit\\.si/TutorMatch-{SessionId:N}-[0-9a-f]{{12}}$");
        second.Should().NotBe(first);
    }
}
