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

        _context.Tutors.Add(tutor);
        _context.SaveChanges();
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
            existing.UpdateSubjects(tutor.TutorSubjects.Select(ts => ts.Subject).Where(s => s is not null));
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
    }

    public bool Delete(Guid id)
    {
        var tutor = _context.Tutors.Find(id);
        if (tutor is null)
        {
            return false;
        }

        _context.Tutors.Remove(tutor);
        _context.SaveChanges();
        return true;
    }

    public Subject GetOrCreateSubject(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var trimmed = name.Trim();
        var existing = _context.Subjects.FirstOrDefault(s => s.Name.ToLower() == trimmed.ToLower());
        if (existing is not null)
        {
            return existing;
        }

        var newSubject = new Subject(trimmed);
        _context.Subjects.Add(newSubject);
        _context.SaveChanges();
        return newSubject;
    }
}
