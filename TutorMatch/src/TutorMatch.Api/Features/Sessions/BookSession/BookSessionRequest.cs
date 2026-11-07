namespace TutorMatch.Api.Features.Sessions.BookSession;

// API request DTOs are not unit-tested and are excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record BookSessionRequest(
    Guid TutorId,
    DateTime StartTime,
    DateTime EndTime);
