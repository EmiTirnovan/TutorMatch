namespace TutorMatch.Api.Domain.Tutors;

// Entitatea pentru materii (ex: Algoritmi, POO)
public class Subject
{
    private readonly List<TutorSubject> _tutorSubjects = new();

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    // Expunem lista doar ca read-only ca sa nu poata fi modificata din exterior
    public IReadOnlyList<TutorSubject> TutorSubjects => _tutorSubjects.AsReadOnly();

    private Subject() { } // pt EF Core

    public Subject(string name, string? description = null, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Numele materiei nu poate fi gol.", nameof(name));
        }

        Id = id ?? Guid.NewGuid();
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void Update(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Numele materiei nu poate fi gol.", nameof(name));
        }

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}
