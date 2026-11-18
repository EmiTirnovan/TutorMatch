using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Features.Sessions.BookSession;

// API request DTOs are not unit-tested and are excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record BookSessionRequest(
    Guid TutorId,
    DateTime StartTime,
    DateTime EndTime,
    string? Subject = null,
    string? Notes = null,
    SessionType Type = SessionType.Online,
    string? LocationName = null,
    double? Latitude = null,
    double? Longitude = null);
