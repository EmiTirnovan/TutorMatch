namespace TutorMatch.Api.Features.Auth.Google;

public record GoogleAuthRequest(
    string? IdToken = null,
    string? Credential = null,
    string? Role = "Student")
{
    public string Token => !string.IsNullOrWhiteSpace(IdToken) ? IdToken : (Credential ?? string.Empty);
}
