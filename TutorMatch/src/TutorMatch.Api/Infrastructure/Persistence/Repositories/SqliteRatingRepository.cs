using Microsoft.EntityFrameworkCore;
using TutorMatch.Api.Domain.Tutors;

namespace TutorMatch.Api.Infrastructure.Persistence.Repositories;

// SQLite persistence implementation is infrastructure glue and is excluded from unit-test coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class SqliteRatingRepository : IRatingRepository
{
    private readonly TutorMatchDbContext _context;

    public SqliteRatingRepository(TutorMatchDbContext context)
    {
        _context = context;
    }

    public IReadOnlyList<Rating> GetByTutorId(Guid tutorId)
    {
        return _context.Ratings
            .AsNoTracking()
            .Where(r => r.TutorId == tutorId)
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<Rating> GetBySessionIds(IEnumerable<Guid> sessionIds)
    {
        ArgumentNullException.ThrowIfNull(sessionIds);

        var ids = sessionIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return Array.Empty<Rating>();
        }

        return _context.Ratings
            .AsNoTracking()
            .Where(r => ids.Contains(r.SessionId))
            .ToList()
            .AsReadOnly();
    }
}
