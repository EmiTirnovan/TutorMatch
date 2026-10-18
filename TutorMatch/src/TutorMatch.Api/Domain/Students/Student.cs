using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Domain.Students;

public class Student
{
    private readonly List<Session> _sessions = new();

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public IReadOnlyList<Session> Sessions => _sessions.AsReadOnly();

    private Student()
    {
    }

    public Student(Guid id, string name = "", string email = "")
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Student ID cannot be empty.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(email);

        Id = id;
        Name = name;
        Email = email;
    }

    public Session BookSession(Guid tutorId, DateRange interval)
    {
        if (tutorId == Guid.Empty)
        {
            throw new ArgumentException("Tutor ID cannot be empty.", nameof(tutorId));
        }

        if (tutorId == Id)
        {
            throw new ArgumentException("Student cannot book a session with themselves as tutor.", nameof(tutorId));
        }

        ArgumentNullException.ThrowIfNull(interval);

        var hasOverlap = _sessions.Any(s => s.Status == SessionStatus.Booked && s.Interval.Overlaps(interval));
        if (hasOverlap)
        {
            throw new InvalidOperationException("Student already has a booked session in this time interval.");
        }

        var session = Session.Create(tutorId, Id, interval);
        _sessions.Add(session);
        return session;
    }

    public void AddSession(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.StudentId != Id)
        {
            throw new InvalidOperationException($"Session belongs to student '{session.StudentId}', not '{Id}'.");
        }

        if (_sessions.Any(s => s.Id == session.Id))
        {
            throw new InvalidOperationException($"Session with ID '{session.Id}' is already associated with this student.");
        }

        _sessions.Add(session);
    }

    public void CancelSession(Guid sessionId)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session == null)
        {
            throw new KeyNotFoundException($"Session with ID '{sessionId}' was not found for this student.");
        }

        session.Cancel();
    }
}