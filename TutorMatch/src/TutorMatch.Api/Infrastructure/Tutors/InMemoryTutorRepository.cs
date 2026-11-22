using System.Collections.Concurrent;
using TutorMatch.Api.Domain.Tutors;

namespace TutorMatch.Api.Infrastructure.Tutors;

// In-memory repository wiring is not a business rule and is excluded from unit-test coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class InMemoryTutorRepository : ITutorRepository
{
    private readonly ConcurrentDictionary<Guid, Tutor> _tutors = new();
    private readonly ConcurrentDictionary<string, Subject> _subjects = new(StringComparer.OrdinalIgnoreCase);

    public void Add(Tutor tutor)
    {
        ArgumentNullException.ThrowIfNull(tutor);

        if (tutor.Id == Guid.Empty)
        {
            throw new ArgumentException("Id-ul tutorelui nu poate fi gol.", nameof(tutor));
        }

        // Respingem id-urile duplicate
        if (!_tutors.TryAdd(tutor.Id, tutor))
        {
            throw new InvalidOperationException($"Un tutore cu Id-ul '{tutor.Id}' exista deja.");
        }

        foreach (var ts in tutor.TutorSubjects)
        {
            var subject = ts.Subject ?? _subjects.Values.FirstOrDefault(s => s.Id == ts.SubjectId);
            subject?.IncrementUsage(1);
        }
    }

    public Tutor? GetById(Guid id)
    {
        return _tutors.TryGetValue(id, out var tutor) ? tutor : null;
    }

    public IReadOnlyList<Tutor> GetAll()
    {
        return _tutors.Values.ToList().AsReadOnly();
    }

    public void Update(Tutor tutor)
    {
        ArgumentNullException.ThrowIfNull(tutor);

        if (!_tutors.TryGetValue(tutor.Id, out var existing))
        {
            throw new KeyNotFoundException($"Tutorele cu Id-ul '{tutor.Id}' nu a fost gasit.");
        }

        var oldSubjectIds = existing.TutorSubjects.Select(ts => ts.SubjectId).ToHashSet();
        var newSubjectIds = tutor.TutorSubjects.Select(ts => ts.SubjectId).ToHashSet();

        var addedIds = newSubjectIds.Except(oldSubjectIds);
        var removedIds = oldSubjectIds.Except(newSubjectIds);

        foreach (var id in addedIds)
        {
            var subject = _subjects.Values.FirstOrDefault(s => s.Id == id);
            subject?.IncrementUsage(1);
        }

        foreach (var id in removedIds)
        {
            var subject = _subjects.Values.FirstOrDefault(s => s.Id == id);
            subject?.DecrementUsage(1);
        }

        _tutors[tutor.Id] = tutor;
    }

    public bool Delete(Guid id)
    {
        if (_tutors.TryRemove(id, out var tutor))
        {
            foreach (var ts in tutor.TutorSubjects)
            {
                var subject = ts.Subject ?? _subjects.Values.FirstOrDefault(s => s.Id == ts.SubjectId);
                subject?.DecrementUsage(1);
            }
            return true;
        }

        return false;
    }

    public Subject GetOrCreateSubject(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var normalized = Subject.NormalizeName(name);
        return _subjects.GetOrAdd(normalized, n => new Subject(n, createdAt: DateTime.UtcNow, usageCount: 0));
    }
}
