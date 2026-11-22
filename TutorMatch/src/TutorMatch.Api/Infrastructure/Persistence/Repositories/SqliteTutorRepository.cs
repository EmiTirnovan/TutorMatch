using Microsoft.EntityFrameworkCore;
using TutorMatch.Api.Domain.Tutors;

namespace TutorMatch.Api.Infrastructure.Persistence.Repositories;

// SQLite persistence implementation is infrastructure glue and is excluded from unit-test coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class SqliteTutorRepository : ITutorRepository
{
    private readonly TutorMatchDbContext _context;

    public SqliteTutorRepository(TutorMatchDbContext context)
    {
        _context = context;
    }

    public void Add(Tutor tutor)
    {
        ArgumentNullException.ThrowIfNull(tutor);

        if (tutor.Id == Guid.Empty)
        {
            throw new ArgumentException("Tutor ID cannot be empty.", nameof(tutor));
        }

        if (_context.Tutors.Any(t => t.Id == tutor.Id))
        {
            throw new InvalidOperationException($"A tutor with ID '{tutor.Id}' already exists.");
        }

        using var transaction = _context.Database.BeginTransaction();
        try
        {
            foreach (var ts in tutor.TutorSubjects)
            {
                var subject = ts.Subject ?? _context.Subjects.Find(ts.SubjectId);
                subject?.IncrementUsage(1);
            }

            _context.Tutors.Add(tutor);
            _context.SaveChanges();
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public Tutor? GetById(Guid id)
    {
        return _context.Tutors
            .Include(t => t.Availability)
            .Include(t => t.TutorSubjects)
                .ThenInclude(ts => ts.Subject)
            .AsSplitQuery()
            .FirstOrDefault(t => t.Id == id);
    }

    public IReadOnlyList<Tutor> GetAll()
    {
        return _context.Tutors
            .Include(t => t.Availability)
            .Include(t => t.TutorSubjects)
                .ThenInclude(ts => ts.Subject)
            .AsSplitQuery()
            .ToList()
            .AsReadOnly();
    }

    public void Update(Tutor tutor)
    {
        ArgumentNullException.ThrowIfNull(tutor);

        using var transaction = _context.Database.BeginTransaction();
        try
        {
            var oldSubjectIds = _context.TutorSubjects
                .AsNoTracking()
                .Where(ts => ts.TutorId == tutor.Id)
                .Select(ts => ts.SubjectId)
                .ToHashSet();

            var newSubjectIds = tutor.TutorSubjects.Select(ts => ts.SubjectId).ToHashSet();

            var addedIds = newSubjectIds.Except(oldSubjectIds).ToList();
            var removedIds = oldSubjectIds.Except(newSubjectIds).ToList();

            foreach (var id in addedIds)
            {
                var subject = _context.Subjects.Find(id);
                subject?.IncrementUsage(1);
            }

            foreach (var id in removedIds)
            {
                var subject = _context.Subjects.Find(id);
                subject?.DecrementUsage(1);
            }

            var entry = _context.Entry(tutor);
            if (entry.State == EntityState.Detached)
            {
                var existing = _context.Tutors
                    .Include(t => t.Availability)
                    .Include(t => t.TutorSubjects)
                        .ThenInclude(ts => ts.Subject)
                    .FirstOrDefault(t => t.Id == tutor.Id);

                if (existing is null)
                {
                    throw new KeyNotFoundException($"Tutor with ID '{tutor.Id}' was not found.");
                }

                existing.UpdateProfile(tutor.Name, tutor.Bio, tutor.HourlyRate);
                existing.UpdateAvailability(tutor.Availability);

                var subjects = tutor.TutorSubjects
                    .Select(ts => ts.Subject ?? _context.Subjects.Find(ts.SubjectId))
                    .Where(s => s is not null)
                    .Select(s => s!);
                existing.UpdateSubjects(subjects);
            }
            else
            {
                var exists = _context.Tutors.Any(t => t.Id == tutor.Id);
                if (!exists)
                {
                    throw new KeyNotFoundException($"Tutor with ID '{tutor.Id}' was not found.");
                }
            }

            _context.SaveChanges();
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public bool Delete(Guid id)
    {
        using var transaction = _context.Database.BeginTransaction();
        try
        {
            var tutor = _context.Tutors
                .Include(t => t.TutorSubjects)
                    .ThenInclude(ts => ts.Subject)
                .FirstOrDefault(t => t.Id == id);

            if (tutor is null)
            {
                return false;
            }

            foreach (var ts in tutor.TutorSubjects)
            {
                var subject = ts.Subject ?? _context.Subjects.Find(ts.SubjectId);
                subject?.DecrementUsage(1);
            }

            _context.Tutors.Remove(tutor);
            _context.SaveChanges();
            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public Subject GetOrCreateSubject(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var normalized = Subject.NormalizeName(name);

        var local = _context.Subjects.Local.FirstOrDefault(s => string.Equals(s.Name, normalized, StringComparison.OrdinalIgnoreCase));
        if (local is not null)
        {
            return local;
        }

        var existing = _context.Subjects.FirstOrDefault(s => EF.Functions.Collate(s.Name, "NOCASE") == normalized || s.Name == normalized);
        if (existing is not null)
        {
            return existing;
        }

        var newSubject = new Subject(normalized, createdAt: DateTime.UtcNow, usageCount: 0);
        _context.Subjects.Add(newSubject);
        _context.SaveChanges();
        return newSubject;
    }
}
