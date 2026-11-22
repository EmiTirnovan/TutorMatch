namespace TutorMatch.Tests.Common;

/// <summary>
/// Polls an asynchronous condition until it holds or the timeout elapses.
/// Used to observe work that the API performs on background tasks (e.g. embedding regeneration).
/// </summary>
public static class Eventually
{
    public static async Task<bool> SatisfiedAsync(
        Func<Task<bool>> condition,
        TimeSpan timeout,
        TimeSpan? pollInterval = null)
    {
        var interval = pollInterval ?? TimeSpan.FromMilliseconds(25);
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
            {
                return true;
            }

            await Task.Delay(interval);
        }

        return await condition();
    }
}
