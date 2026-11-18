using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using TutorMatch.Api.Infrastructure.Meetings;
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

    private static (GoogleMeetService Service, StubHandler Handler) CreateGoogleService(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new StubHandler(respond);
        var options = Options.Create(new GoogleMeetOptions { ClientId = "client", ClientSecret = "secret", RefreshToken = "refresh" });
        return (new GoogleMeetService(new StubHttpClientFactory(new HttpClient(handler)), options), handler);
    }

    [Fact]
    public async Task Given_googleReturnsMeetLink_When_generatingLink_Then_returnsHangoutLinkAndSendsConferenceRequest()
    {
        var (service, handler) = CreateGoogleService(request =>
            request.RequestUri!.Host == "oauth2.googleapis.com"
                ? Json("""{"access_token":"token-123","expires_in":3599}""")
                : Json("""{"id":"evt1","hangoutLink":"https://meet.google.com/abc-defg-hij"}"""));

        var link = await service.GenerateMeetingLinkAsync(SessionId, "TutorMatch: Algebra", Start, End);

        link.Should().Be("https://meet.google.com/abc-defg-hij");
        handler.Requests.Should().HaveCount(2);
        handler.Requests[0].Body.Should().Contain("grant_type=refresh_token").And.Contain("refresh_token=refresh");
        var (eventRequest, eventBody) = handler.Requests[1];
        eventRequest.RequestUri!.ToString().Should().Contain("/calendars/primary/events").And.Contain("conferenceDataVersion=1");
        eventRequest.Headers.Authorization!.ToString().Should().Be("Bearer token-123");
        eventBody.Should().Contain("hangoutsMeet").And.Contain(SessionId.ToString()).And.Contain("TutorMatch: Algebra");
    }

    [Fact]
    public async Task Given_eventWithoutHangoutLink_When_generatingLink_Then_throwsInvalidOperationException()
    {
        var (service, _) = CreateGoogleService(request =>
            request.RequestUri!.Host == "oauth2.googleapis.com"
                ? Json("""{"access_token":"token-123"}""")
                : Json("""{"id":"evt1"}"""));

        var action = () => service.GenerateMeetingLinkAsync(SessionId, "Topic", Start, End);

        await action.Should().ThrowAsync<InvalidOperationException>();
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
