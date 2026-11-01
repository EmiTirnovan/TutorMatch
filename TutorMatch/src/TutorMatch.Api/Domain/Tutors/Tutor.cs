namespace TutorMatch.Api.Domain.Tutors;

using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Tutors.Events;

// Aggregate root for Tutor
public class Tutor
{
    // Internal collections are private to protect domain invariants
    private readonly List<DateRange> _availability = new();
    private readonly List<TutorSubject> _tutorSubjects = new();
    private readonly List<object> _domainEvents = new();

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Bio { get; private set; } = string.Empty;
    public decimal HourlyRate { get; private set; }

    // Exposed as IReadOnlyList so state cannot be mutated outside the aggregate
    public IReadOnlyList<DateRange> Availability => _availability.AsReadOnly();
    public IReadOnlyList<TutorSubject> TutorSubjects => _tutorSubjects.AsReadOnly();
    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();

    private Tutor() { } // Required for EF Core

    public Tutor(
        string name,
        string email,
        string bio,
        decimal hourlyRate,
        Guid? id = null)
    {
        ValidateProfile(name, email, bio, hourlyRate);

        Id = id ?? Guid.NewGuid();
        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        Bio = bio.Trim();
        HourlyRate = hourlyRate;
    }

    public void UpdateProfile(string name, string bio, decimal hourlyRate)
    {
        ValidateProfile(name, Email, bio, hourlyRate);

        Name = name.Trim();
        Bio = bio.Trim();
        HourlyRate = hourlyRate;
    }

    // Adds a new availability slot if it does not overlap with existing slots
    public void AddAvailability(DateRange range)
    {
        ArgumentNullException.ThrowIfNull(range);

        if (_availability.Any(existing => existing.Overlaps(range)))
        {
            throw new InvalidOperationException("Intervalul de disponibilitate se suprapune cu unul deja existent.");
        }

        _availability.Add(range);
    }

    public bool RemoveAvailability(DateRange range)
    {
        ArgumentNullException.ThrowIfNull(range);
        return _availability.Remove(range);
    }

    // Replaces all availability slots after validating that no two slots in the new list overlap
    public void UpdateAvailability(IEnumerable<DateRange> newAvailability)
    {
        ArgumentNullException.ThrowIfNull(newAvailability);

        var rangesList = newAvailability.ToList();

        for (int i = 0; i < rangesList.Count; i++)
        {
            for (int j = i + 1; j < rangesList.Count; j++)
            {
                if (rangesList[i].Overlaps(rangesList[j]))
                {
                    throw new InvalidOperationException(
                        $"Intervalele {rangesList[i]} si {rangesList[j]} se suprapun.");
                }
            }
        }

        _availability.Clear();
        _availability.AddRange(rangesList);
    }

    // Checks whether the requested range fits entirely within any available slot
    public bool IsAvailableDuring(DateRange requestedRange)
    {
        ArgumentNullException.ThrowIfNull(requestedRange);
        return _availability.Any(slot => slot.Contains(requestedRange));
    }

    public void AddSubject(Subject subject)
    {
        ArgumentNullException.ThrowIfNull(subject);

        if (_tutorSubjects.Any(ts => ts.SubjectId == subject.Id))
        {
            throw new InvalidOperationException($"Materia '{subject.Name}' este deja asociata acestui tutore.");
        }

        _tutorSubjects.Add(new TutorSubject(this, subject));
    }

    public void AddSubject(Guid subjectId)
    {
        if (subjectId == Guid.Empty)
        {
            throw new ArgumentException("SubjectId nu poate fi gol.", nameof(subjectId));
        }

        if (_tutorSubjects.Any(ts => ts.SubjectId == subjectId))
        {
            throw new InvalidOperationException("Materia este deja asociata acestui tutore.");
        }

        _tutorSubjects.Add(new TutorSubject(Id, subjectId));
    }

    public bool RemoveSubject(Guid subjectId)
    {
        var item = _tutorSubjects.FirstOrDefault(ts => ts.SubjectId == subjectId);
        if (item is null)
        {
            return false;
        }

        return _tutorSubjects.Remove(item);
    }

    // Appends a domain event to the internal collection
    public void AddDomainEvent(object domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    // Clears all recorded domain events
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    // Creates a TutorSavedNotification for in-process consumers such as AI embedding generation
    public TutorSavedNotification CreateTutorSavedNotification(IEnumerable<string>? subjectNames = null)
    {
        var subjects = subjectNames?.ToList() ?? _tutorSubjects
            .Select(ts => ts.Subject?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToList();

        return new TutorSavedNotification(Id, Bio, subjects);
    }

    private static void ValidateProfile(string name, string email, string bio, decimal hourlyRate)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Numele tutorelui nu poate fi gol.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new ArgumentException("Adresa de email este invalida.", nameof(email));
        }

        if (bio is null)
        {
            throw new ArgumentNullException(nameof(bio), "Biografia nu poate fi null.");
        }

        if (hourlyRate <= 0)
        {
            throw new ArgumentException("Tariful orar trebuie sa fie pozitiv.", nameof(hourlyRate));
        }
    }
}
