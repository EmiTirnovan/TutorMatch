namespace TutorMatch.Api.Features.Auth;

public record AuthResponse(
    string Token,
    Guid UserId,
    string Email,
    string FullName,
    string Role);
