using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Features.Sessions.CreateSession;

public record CreateSessionResponse(
    Guid Id,
    Guid TutorId,
    Guid StudentId,
    DateTime StartTime,
    DateTime EndTime,
    SessionStatus Status,
    int Version);
