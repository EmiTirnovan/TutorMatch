using TutorMatch.Api.Domain.Common;

namespace TutorMatch.Api.Domain.Sessions;

public enum SessionStatus
{
    Booked,
    Cancelled,
    LateCancelled,
    Completed
}

public class Session
{
    public Guid Id { get; private set; }
    public Guid TutorId { get; private set; }
    public Guid StudentId { get; private set; }
    public DateRange Interval { get; private set; } = null!;
    public DateRange DateRange => Interval;
    public DateTime StartTime => Interval.Start;
    public DateTime EndTime => Interval.End;
    public SessionStatus Status { get; private set; } = SessionStatus.Booked;
    public int Version { get; private set; } = 1;

    // Private constructor for persistence/framework deserialization
    private Session()
    {
    }

    public Session(
        Guid id,
        Guid tutorId,
        Guid studentId,
        DateRange interval,
        SessionStatus status = SessionStatus.Booked,
        int version = 1)
    {
        ArgumentNullException.ThrowIfNull(interval);

        if (version < 1)
        {
            throw new ArgumentException("Version must be greater than or equal to 1.", nameof(version));
        }

        if (!Enum.IsDefined(typeof(SessionStatus), status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Invalid session status.");
        }

        Id = id;
        TutorId = tutorId;
        StudentId = studentId;
        Interval = interval;
        Status = status;
        Version = version;
    }

    public Session(
        Guid id,
        Guid tutorId,
        Guid studentId,
        DateTime startTime,
        DateTime endTime,
        SessionStatus status = SessionStatus.Booked,
        int version = 1)
        : this(id, tutorId, studentId, new DateRange(startTime, endTime), status, version)
    {
    }

    public static Session Create(
        Guid tutorId,
        Guid studentId,
        DateRange interval)
    {
        return new Session(
            Guid.NewGuid(),
            tutorId,
            studentId,
            interval,
            SessionStatus.Booked,
            1);
    }

    public void Cancel()
    {
        if (Status != SessionStatus.Booked)
        {
            throw new InvalidOperationException($"Cannot cancel a session with status '{Status}'. Only booked sessions can be cancelled.");
        }

        Status = SessionStatus.Cancelled;
        Version++;
    }

    public void LateCancel()
    {
        if (Status != SessionStatus.Booked)
        {
            throw new InvalidOperationException($"Cannot late cancel a session with status '{Status}'. Only booked sessions can be late cancelled.");
        }

        Status = SessionStatus.LateCancelled;
        Version++;
    }

    public void Complete()
    {
        if (Status != SessionStatus.Booked)
        {
            throw new InvalidOperationException($"Cannot complete a session with status '{Status}'. Only booked sessions can be completed.");
        }

        Status = SessionStatus.Completed;
        Version++;
    }

    public void Reschedule(DateRange newInterval)
    {
        ArgumentNullException.ThrowIfNull(newInterval);

        if (Status != SessionStatus.Booked)
        {
            throw new InvalidOperationException($"Cannot reschedule a session with status '{Status}'. Only booked sessions can be rescheduled.");
        }

        Interval = newInterval;
        Version++;
    }

    public void Reschedule(DateTime newStartTime, DateTime newEndTime)
    {
        Reschedule(new DateRange(newStartTime, newEndTime));
    }
}
