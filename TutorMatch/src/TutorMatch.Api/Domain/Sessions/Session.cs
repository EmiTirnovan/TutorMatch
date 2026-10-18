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
    public Guid Id { get; init; }
    public Guid TutorId { get; set; }
    public Guid StudentId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Booked;
    public int Version { get; set; } = 1;

    public Session()
    {
    }

    public Session(
        Guid id,
        Guid tutorId,
        Guid studentId,
        DateTime startTime,
        DateTime endTime,
        SessionStatus status = SessionStatus.Booked,
        int version = 1)
    {
        Id = id;
        TutorId = tutorId;
        StudentId = studentId;
        StartTime = startTime;
        EndTime = endTime;
        Status = status;
        Version = version;
    }
}