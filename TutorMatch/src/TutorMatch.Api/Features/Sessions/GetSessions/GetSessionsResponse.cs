using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Features.Sessions.GetSessions;

public record GetSessionsResponse(
    Guid Id,
    Guid TutorId,
    Guid StudentId,
    DateTime StartTime,
    DateTime EndTime,
    SessionStatus Status,
    int Version);
