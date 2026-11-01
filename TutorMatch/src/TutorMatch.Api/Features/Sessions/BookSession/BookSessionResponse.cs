using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Features.Sessions.BookSession;

// API response DTOs are not unit-tested and are excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record BookSessionResponse(
    Guid Id,
    Guid TutorId,
    Guid StudentId,
    DateTime StartTime,
    DateTime EndTime,
    SessionStatus Status,
    int Version);
