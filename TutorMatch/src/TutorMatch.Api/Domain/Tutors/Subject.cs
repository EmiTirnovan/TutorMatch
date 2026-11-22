using System.Text.RegularExpressions;

namespace TutorMatch.Api.Domain.Tutors;

// Centralized Subjects catalog entity
public class Subject
{
    private readonly List<TutorSubject> _tutorSubjects = new();

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int UsageCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? Description { get; private set; }

    // Exposed as read-only so collection cannot be mutated externally
    public IReadOnlyList<TutorSubject> TutorSubjects => _tutorSubjects.AsReadOnly();

    private Subject() { } // Required for EF Core

    public Subject(
        string name,
        string? description = null,
        Guid? id = null,
        DateTime? createdAt = null,
        int usageCount = 0)
    {
        Id = id ?? Guid.NewGuid();
        Name = NormalizeName(name);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        CreatedAt = createdAt ?? DateTime.UtcNow;
        UsageCount = Math.Max(0, usageCount);
    }

    public static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Numele materiei nu poate fi gol.", nameof(name));
        }

        var trimmed = name.Trim();
        return Regex.Replace(trimmed, @"\s+", " ");
    }

    public void IncrementUsage(int count = 1)
    {
        if (count <= 0) return;
        UsageCount += count;
    }

    public void DecrementUsage(int count = 1)
    {
        if (count <= 0) return;
        UsageCount = Math.Max(0, UsageCount - count);
    }

    public void SetUsageCount(int count)
    {
        UsageCount = Math.Max(0, count);
    }

    public void Update(string name, string? description)
    {
        Name = NormalizeName(name);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}
