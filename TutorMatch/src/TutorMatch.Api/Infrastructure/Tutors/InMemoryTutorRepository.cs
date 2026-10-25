using System.Collections.Concurrent;
using TutorMatch.Api.Domain.Tutors;

namespace TutorMatch.Api.Infrastructure.Tutors;

// In-memory repository wiring is not a business rule and is excluded from unit-test coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class InMemoryTutorRepository : ITutorRepository
{
    private readonly ConcurrentDictionary<Guid, Tutor> _tutors = new();

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
    }

    public Tutor? GetById(Guid id)
    {
        // Tratare safe daca nu exista: returneaza null fara crash
        return _tutors.TryGetValue(id, out var tutor) ? tutor : null;
    }

    public IReadOnlyList<Tutor> GetAll()
    {
        // Snapshot izolat (copie) ca sa evitam erori de concurenta la iterare
        return _tutors.Values.ToList().AsReadOnly();
    }

    public void Update(Tutor tutor)
    {
        ArgumentNullException.ThrowIfNull(tutor);

        if (!_tutors.ContainsKey(tutor.Id))
        {
            throw new KeyNotFoundException($"Tutorele cu Id-ul '{tutor.Id}' nu a fost gasit.");
        }

        _tutors[tutor.Id] = tutor;
    }

    public bool Delete(Guid id)
    {
        // Daca exista il sterge si da true, altfel da false
        return _tutors.TryRemove(id, out _);
    }
}
