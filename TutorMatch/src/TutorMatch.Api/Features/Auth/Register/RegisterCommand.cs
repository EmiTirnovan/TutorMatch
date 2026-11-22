using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Auth.Register;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record RegisterCommand(
    string Email,
    string Password,
    string FullName,
    string? Role = "Student") : ICommand<Result<AuthResponse>>;
