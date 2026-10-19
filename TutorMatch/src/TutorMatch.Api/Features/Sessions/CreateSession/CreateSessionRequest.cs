namespace TutorMatch.Api.Features.Sessions.CreateSession;

public record CreateSessionRequest(
    Guid TutorId,
    Guid StudentId,
    DateTime StartTime,
    DateTime EndTime);
