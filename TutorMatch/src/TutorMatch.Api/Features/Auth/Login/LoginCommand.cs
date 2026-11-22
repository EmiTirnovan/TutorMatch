using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Auth.Login;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record LoginCommand(
    string Email,
    string Password) : ICommand<Result<AuthResponse>>;
