using TutorMatch.Api.Domain.Common;

namespace TutorMatch.Api.Domain.Sessions;

public enum SessionStatus
{
    Booked,
    Cancelled,
    LateCancelled,
    Completed
}

public enum SessionType
{
    Online,
    Physical
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
    public string? Subject { get; private set; }
    public string? Notes { get; private set; }
    public SessionType Type { get; private set; } = SessionType.Online;
    public string? MeetingLink { get; private set; }
    public string? LocationName { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }

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
        int version = 1,
        string? subject = null,
        string? notes = null,
        SessionType type = SessionType.Online,
        string? meetingLink = null,
        string? locationName = null,
        double? latitude = null,
        double? longitude = null)
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
        Subject = string.IsNullOrWhiteSpace(subject) ? null : subject.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        SetMode(type, meetingLink, locationName, latitude, longitude);
    }

    public Session(
        Guid id,
        Guid tutorId,
        Guid studentId,
        DateTime startTime,
        DateTime endTime,
        SessionStatus status = SessionStatus.Booked,
        int version = 1,
        string? subject = null,
        string? notes = null)
        : this(id, tutorId, studentId, new DateRange(startTime, endTime), status, version, subject, notes)
    {
    }

    public static Session Create(
        Guid tutorId,
        Guid studentId,
        DateRange interval,
        string? subject = null,
        string? notes = null,
        SessionType type = SessionType.Online,
        string? meetingLink = null,
        string? locationName = null,
        double? latitude = null,
        double? longitude = null)
    {
        return new Session(
            Guid.NewGuid(),
            tutorId,
            studentId,
            interval,
            SessionStatus.Booked,
            1,
            subject,
            notes,
            type,
            meetingLink,
            locationName,
            latitude,
            longitude);
    }

    // shortcut: an Online session may have no MeetingLink (legacy rows, direct construction);
    // no link is generated yet (pending IMeetingService). Make it mandatory once that lands and legacy rows are backfilled.
    private void SetMode(SessionType type, string? meetingLink, string? locationName, double? latitude, double? longitude)
    {
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), "Invalid session type.");
        }

        if (type == SessionType.Online)
        {
            Type = type;
            MeetingLink = string.IsNullOrWhiteSpace(meetingLink) ? null : meetingLink.Trim();
            return;
        }

        if (string.IsNullOrWhiteSpace(locationName))
        {
            throw new ArgumentException("LocationName is required for physical sessions.", nameof(locationName));
        }

        if (latitude.HasValue != longitude.HasValue)
        {
            throw new ArgumentException("Latitude and Longitude must be provided together.", nameof(latitude));
        }

        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "GPS coordinates are out of range.");
        }

        Type = type;
        LocationName = locationName.Trim();
        Latitude = latitude;
        Longitude = longitude;
    }

    public void UpdateDetails(string? subject, string? notes)
    {
        Subject = string.IsNullOrWhiteSpace(subject) ? null : subject.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
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

    public void Cancel(DateTime utcNow)
    {
        if (Status != SessionStatus.Booked)
        {
            throw new InvalidOperationException($"Cannot cancel a session with status '{Status}'. Only booked sessions can be cancelled.");
        }

        var deadline = Interval.Start.AddHours(-12);
        if (utcNow <= deadline)
        {
            Status = SessionStatus.Cancelled;
        }
        else
        {
            Status = SessionStatus.LateCancelled;
        }

        Version++;
    }

    public void Cancel(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        Cancel(timeProvider.GetUtcNow().UtcDateTime);
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

    public void Complete(DateTime utcNow)
    {
        if (Status != SessionStatus.Booked)
        {
            throw new InvalidOperationException($"Cannot complete a session with status '{Status}'. Only booked sessions can be completed.");
        }

        if (utcNow < Interval.Start)
        {
            throw new InvalidOperationException("Cannot complete a session before its start time.");
        }

        Status = SessionStatus.Completed;
        Version++;
    }

    public void Complete(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        Complete(timeProvider.GetUtcNow().UtcDateTime);
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

    public void IncrementVersion()
    {
        Version++;
    }

    public void VerifyVersion(int expectedVersion)
    {
        if (Version != expectedVersion)
        {
            throw new InvalidOperationException($"Concurrency conflict: expected version {expectedVersion}, but found {Version}.");
        }
    }
}
