using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Domain.Tutors;

public class Tutor
{
    private readonly List<Session> _sessions = new();

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public IReadOnlyCollection<Session> Sessions => _sessions.AsReadOnly();

    private Tutor()
    {
    }

    public Tutor(Guid id, string name = "", string email = "")
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Tutor ID cannot be empty.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(email);

        Id = id;
        Name = name;
        Email = email;
    }

    public void AddSession(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.TutorId != Id)
        {
            throw new InvalidOperationException($"Session belongs to tutor '{session.TutorId}', not '{Id}'.");
        }

        if (_sessions.Any(s => s.Id == session.Id))
        {
            throw new InvalidOperationException($"Session with ID '{session.Id}' is already associated with this tutor.");
        }

        _sessions.Add(session);
    }

    public void CancelSession(Guid sessionId)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session == null)
        {
            throw new KeyNotFoundException($"Session with ID '{sessionId}' was not found for this tutor.");
        }

        session.Cancel();
    }
}
