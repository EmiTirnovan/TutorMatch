namespace TutorMatch.Api.Features.Auth.Register;

public record RegisterRequest(
    string Email,
    string Password,
    string FullName,
    string? Role = "Student");
