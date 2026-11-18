using System.Security.Cryptography;
using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Infrastructure.Meetings;

/// <summary>
/// Fallback used when Google Meet credentials are not configured (e.g. on a teammate's machine):
/// a working Jitsi room derived from the session ID plus a random suffix, so it cannot be guessed.
/// </summary>
public class JitsiMeetingService : IMeetingService
{
    public Task<string> GenerateMeetingLinkAsync(Guid sessionId, string topic, DateTime start, DateTime end) =>
        Task.FromResult($"https://meet.jit.si/TutorMatch-{sessionId:N}-{RandomNumberGenerator.GetHexString(12, lowercase: true)}");
}
