using System.Collections.Concurrent;
using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Infrastructure.Sessions;

public class InMemorySessionRepository : ISessionRepository
{
    private readonly ConcurrentDictionary<Guid, Session> _sessions = new();

    public void Add(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.Id == Guid.Empty)
        {
            throw new ArgumentException("Session ID cannot be empty.", nameof(session));
        }

        if (!_sessions.TryAdd(session.Id, session))
        {
            throw new InvalidOperationException($"A session with ID '{session.Id}' already exists.");
        }
    }

    public Session? GetById(Guid id)
    {
        return _sessions.TryGetValue(id, out var session) ? session : null;
    }

    public IReadOnlyList<Session> GetAll()
    {
        // Create an isolated snapshot of the current values to ensure thread-safety
        // and prevent concurrent modification issues during iteration.
        return _sessions.Values.ToList().AsReadOnly();
    }

    public void Update(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!_sessions.ContainsKey(session.Id))
        {
            throw new KeyNotFoundException($"Session with ID '{session.Id}' was not found.");
        }

        _sessions[session.Id] = session;
    }

    public bool Delete(Guid id)
    {
        return _sessions.TryRemove(id, out _);
    }
}