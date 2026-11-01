using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Discovery.RateSession;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record RateSessionCommand(
    Guid SessionId, 
    Guid StudentId, 
    int Score, 
    string? Notes) : ICommand<Result>;