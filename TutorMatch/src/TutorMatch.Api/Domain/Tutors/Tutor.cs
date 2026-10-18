namespace TutorMatch.Api.Domain.Tutors;

using TutorMatch.Api.Domain.Common;

// Agregatul radacina pentru Tutor
public class Tutor
{
    // Colectiile interne sunt private pentru a proteja invariantele
    private readonly List<DateRange> _availability = new();
    private readonly List<TutorSubject> _tutorSubjects = new();

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Bio { get; private set; } = string.Empty;
    public decimal HourlyRate { get; private set; }

    // Expuse ca IReadOnlyList ca sa nu poata fi alterate din afara agregatului
    public IReadOnlyList<DateRange> Availability => _availability.AsReadOnly();
    public IReadOnlyList<TutorSubject> TutorSubjects => _tutorSubjects.AsReadOnly();

    private Tutor() { } // pt EF Core

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

    // Adaugam un slot nou doar daca nu se suprapune cu cele existente
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

    // Inlocuim toata disponibilitatea, dar verificam inainte sa nu existe suprapuneri in noua lista
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

    // Folosit la rezervare ca sa vedem daca intervalul cerut pica intr-un slot disponibil
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
