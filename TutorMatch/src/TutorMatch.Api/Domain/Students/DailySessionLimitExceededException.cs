namespace TutorMatch.Api.Domain.Students;

// R4: a dedicated type lets handlers map the daily limit to its own error without parsing the message.
public class DailySessionLimitExceededException : InvalidOperationException
{
    public DailySessionLimitExceededException(string message)
        : base(message)
    {
    }
}
