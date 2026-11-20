using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Domain.Students;
using TutorMatch.Api.Domain.Tutors;

namespace TutorMatch.Api.Infrastructure.Meetings;

/// <summary>
/// Creates an open Google Meet space (anyone with the link joins without knocking, so no host has to admit people)
/// and a Google Calendar event that invites the tutor and the student with that link.
/// Authenticates as the platform account through its OAuth refresh token
/// (scopes: meetings.space.created and calendar.events).
/// </summary>
public class GoogleMeetService : IMeetingService
{
    public const string HttpClientName = "GoogleMeet";
    private const string TokenUrl = "https://oauth2.googleapis.com/token";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GoogleMeetOptions _options;
    private readonly ISessionRepository _sessionRepository;
    private readonly ITutorRepository _tutorRepository;
    private readonly IStudentRepository _studentRepository;

    public GoogleMeetService(
        IHttpClientFactory httpClientFactory,
        IOptions<GoogleMeetOptions> options,
        ISessionRepository sessionRepository,
        ITutorRepository tutorRepository,
        IStudentRepository studentRepository)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _sessionRepository = sessionRepository;
        _tutorRepository = tutorRepository;
        _studentRepository = studentRepository;
    }

    public async Task<string> GenerateMeetingLinkAsync(Guid sessionId, string topic, DateTime start, DateTime end)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        var accessToken = await GetAccessTokenAsync(client);

        var space = await PostJsonAsync(client, accessToken,
            "https://meet.googleapis.com/v2/spaces",
            new { config = new { accessType = "OPEN" } });
        var meetingUri = space.TryGetProperty("meetingUri", out var uri) && !string.IsNullOrWhiteSpace(uri.GetString())
            ? uri.GetString()!
            : throw new InvalidOperationException("Google Meet did not return a meeting link for the created space.");

        // The event only carries the invitations; the room itself is the open space above.
        await PostJsonAsync(client, accessToken,
            $"https://www.googleapis.com/calendar/v3/calendars/{Uri.EscapeDataString(_options.CalendarId)}/events?sendUpdates=all",
            new
            {
                summary = topic,
                location = meetingUri,
                description = $"Link Google Meet: {meetingUri}",
                start = new { dateTime = start.ToUniversalTime().ToString("o") },
                end = new { dateTime = end.ToUniversalTime().ToString("o") },
                attendees = GetAttendeeEmails(sessionId).Select(email => new { email }).ToArray()
            });

        return meetingUri;
    }

    private static async Task<JsonElement> PostJsonAsync(HttpClient client, string accessToken, string url, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    // The session is saved before its link is generated, so its tutor and student can be looked up here.
    private IEnumerable<string> GetAttendeeEmails(Guid sessionId)
    {
        var session = _sessionRepository.GetById(sessionId);
        if (session is null)
        {
            return [];
        }

        return new[] { _tutorRepository.GetById(session.TutorId)?.Email, _studentRepository.GetById(session.StudentId)?.Email }
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    // shortcut: fetches a fresh access token per booking (one extra HTTP call); cache it until expiry if booking volume grows.
    private async Task<string> GetAccessTokenAsync(HttpClient client)
    {
        using var response = await client.PostAsync(TokenUrl, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId!,
            ["client_secret"] = _options.ClientSecret!,
            ["refresh_token"] = _options.RefreshToken!,
            ["grant_type"] = "refresh_token"
        }));
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<JsonElement>();
        return token.TryGetProperty("access_token", out var accessToken) && !string.IsNullOrWhiteSpace(accessToken.GetString())
            ? accessToken.GetString()!
            : throw new InvalidOperationException("Google OAuth did not return an access token.");
    }
}
