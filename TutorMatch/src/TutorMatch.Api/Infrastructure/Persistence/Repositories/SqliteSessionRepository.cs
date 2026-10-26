using Microsoft.EntityFrameworkCore;
using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Infrastructure.Persistence.Repositories;

// SQLite persistence implementation is infrastructure glue and is excluded from unit-test coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class SqliteSessionRepository : ISessionRepository
{
    private readonly TutorMatchDbContext _context;

    public SqliteSessionRepository(TutorMatchDbContext context)
    {
        _context = context;
    }

    public void Add(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.Id == Guid.Empty)
        {
            throw new ArgumentException("Session ID cannot be empty.", nameof(session));
        }

        if (_context.Sessions.Any(s => s.Id == session.Id))
        {
            throw new InvalidOperationException($"A session with ID '{session.Id}' already exists.");
        }

        _context.Sessions.Add(session);
        _context.SaveChanges();
    }

    public Session? GetById(Guid id)
    {
        return _context.Sessions.FirstOrDefault(s => s.Id == id);
    }

    public IReadOnlyList<Session> GetAll()
    {
        return _context.Sessions.ToList().AsReadOnly();
    }

    public void Update(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var entry = _context.Entry(session);
        if (entry.State == EntityState.Detached)
        {
            var existing = _context.Sessions.Find(session.Id);
            if (existing is null)
            {
                throw new KeyNotFoundException($"Session with ID '{session.Id}' was not found.");
            }

            _context.Entry(existing).CurrentValues.SetValues(session);
        }
        else
        {
            var exists = _context.Sessions.Any(s => s.Id == session.Id);
            if (!exists)
            {
                throw new KeyNotFoundException($"Session with ID '{session.Id}' was not found.");
            }
        }

        try
        {
            _context.SaveChanges();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("The session was modified concurrently by another process.", ex);
        }
    }

    public bool Delete(Guid id)
    {
        var session = _context.Sessions.Find(id);
        if (session is null)
        {
            return false;
        }

        _context.Sessions.Remove(session);
        _context.SaveChanges();
        return true;
    }
}
