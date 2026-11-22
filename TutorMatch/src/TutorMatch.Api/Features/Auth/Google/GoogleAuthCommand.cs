using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Auth.Google;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record GoogleAuthCommand(
    string Token,
    string? Role = "Student") : ICommand<Result<AuthResponse>>;
