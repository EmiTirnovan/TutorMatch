namespace TutorMatch.Api.Features.Sessions.CompleteSession;

// DTOs and messages contain no business logic and are excluded from unit testing.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record CompleteSessionRequest(Guid TutorId);

