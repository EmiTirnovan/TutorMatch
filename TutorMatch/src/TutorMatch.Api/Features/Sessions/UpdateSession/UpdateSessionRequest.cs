namespace TutorMatch.Api.Features.Sessions.UpdateSession;

public record UpdateSessionRequest(
    DateTime StartTime,
    DateTime EndTime);
