using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Infrastructure.Meetings;

/// <summary>
/// Creates a Google Calendar event with an attached Google Meet conference and returns its link.
/// Authenticates as the platform account through its OAuth refresh token.
/// </summary>
public class GoogleMeetService : IMeetingService
{
    public const string HttpClientName = "GoogleMeet";
    private const string TokenUrl = "https://oauth2.googleapis.com/token";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GoogleMeetOptions _options;

    public GoogleMeetService(IHttpClientFactory httpClientFactory, IOptions<GoogleMeetOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<string> GenerateMeetingLinkAsync(Guid sessionId, string topic, DateTime start, DateTime end)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        var accessToken = await GetAccessTokenAsync(client);

        var calendarEvent = new
        {
            summary = topic,
            start = new { dateTime = start.ToUniversalTime().ToString("o") },
            end = new { dateTime = end.ToUniversalTime().ToString("o") },
            conferenceData = new
            {
                // requestId = sessionId keeps retries for the same session idempotent on Google's side.
                createRequest = new { requestId = sessionId.ToString(), conferenceSolutionKey = new { type = "hangoutsMeet" } }
            }
        };

        var url = $"https://www.googleapis.com/calendar/v3/calendars/{Uri.EscapeDataString(_options.CalendarId)}/events?conferenceDataVersion=1";
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(calendarEvent) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        return created.TryGetProperty("hangoutLink", out var link) && !string.IsNullOrWhiteSpace(link.GetString())
            ? link.GetString()!
            : throw new InvalidOperationException("Google Calendar did not return a Meet link for the created event.");
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
