using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Features.Sessions.GetSessionById;

public record GetSessionByIdResponse(
    Guid Id,
    Guid TutorId,
    Guid StudentId,
    DateTime StartTime,
    DateTime EndTime,
    SessionStatus Status,
    int Version);
