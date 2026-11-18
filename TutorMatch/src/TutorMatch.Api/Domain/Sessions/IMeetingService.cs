namespace TutorMatch.Api.Domain.Sessions;

public interface IMeetingService
{
    Task<string> GenerateMeetingLinkAsync(Guid sessionId, string topic, DateTime start, DateTime end);
}
