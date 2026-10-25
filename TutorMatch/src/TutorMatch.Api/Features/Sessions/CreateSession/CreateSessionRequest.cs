namespace TutorMatch.Api.Features.Sessions.CreateSession;

// API request DTOs are not unit-tested and are excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record CreateSessionRequest(
    Guid TutorId,
    Guid StudentId,
    DateTime StartTime,
    DateTime EndTime);
