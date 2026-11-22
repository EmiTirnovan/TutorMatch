namespace TutorMatch.Api.Features.Auth.Login;

public record LoginRequest(
    string Email,
    string Password);
