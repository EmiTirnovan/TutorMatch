namespace TutorMatch.Api.Infrastructure.Meetings;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class GoogleMeetOptions
{
    public const string SectionName = "GoogleMeet";

    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? RefreshToken { get; set; }
    public string CalendarId { get; set; } = "primary";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret) &&
        !string.IsNullOrWhiteSpace(RefreshToken);
}
