using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Tests.Common.Fakes;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class FakeMeetingService : IMeetingService
{
    public List<(Guid SessionId, string Topic, DateTime Start, DateTime End)> Calls { get; } = new();

    public Exception? FailWith { get; set; }

    public static string LinkFor(Guid sessionId) => $"https://meet.google.com/fake-{sessionId:N}";

    public Task<string> GenerateMeetingLinkAsync(Guid sessionId, string topic, DateTime start, DateTime end)
    {
        Calls.Add((sessionId, topic, start, end));
        return FailWith is null ? Task.FromResult(LinkFor(sessionId)) : Task.FromException<string>(FailWith);
    }
}
