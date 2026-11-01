using TutorMatch.Api.Common.Cqrs;

namespace TutorMatch.Api.Features.Sessions.CompleteSession;

// DTOs and messages contain no business logic and are excluded from unit testing.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record CompleteSessionCommand(Guid SessionId, Guid TutorId) : ICommand;

