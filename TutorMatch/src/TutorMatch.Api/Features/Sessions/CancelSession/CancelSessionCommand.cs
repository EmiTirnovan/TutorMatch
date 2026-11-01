using TutorMatch.Api.Common.Cqrs;

namespace TutorMatch.Api.Features.Sessions.CancelSession;

// DTOs and messages contain no business logic and are excluded from unit testing.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record CancelSessionCommand(Guid SessionId, Guid RequesterId) : ICommand;

