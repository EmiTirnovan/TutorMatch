using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Features.Sessions.GetMySessions;

// API response DTOs are not unit-tested and are excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record GetMySessionsResponse(List<MySessionDto> Sessions);

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record MySessionDto(
    Guid Id,
    Guid TutorId,
    Guid StudentId,
    DateTime StartTime,
    DateTime EndTime,
    SessionStatus Status,
    int Version,
    string? TutorName = null,
    string? StudentName = null);

