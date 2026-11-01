using System.Collections.Concurrent;
using TutorMatch.Api.Domain.Students;

namespace TutorMatch.Api.Infrastructure.Students;

// In-memory repository wiring is not a business rule and is excluded from unit-test coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class InMemoryStudentRepository : IStudentRepository
{
    private readonly ConcurrentDictionary<Guid, Student> _students = new();

    public void Add(Student student)
    {
        ArgumentNullException.ThrowIfNull(student);

        if (student.Id == Guid.Empty)
        {
            throw new ArgumentException("Student ID cannot be empty.", nameof(student));
        }

        if (!_students.TryAdd(student.Id, student))
        {
            throw new InvalidOperationException($"A student with ID '{student.Id}' already exists.");
        }
    }

    public Student? GetById(Guid id)
    {
        return _students.TryGetValue(id, out var student) ? student : null;
    }

    public IReadOnlyList<Student> GetAll()
    {
        return _students.Values.ToList().AsReadOnly();
    }

    public void Update(Student student)
    {
        ArgumentNullException.ThrowIfNull(student);

        if (!_students.ContainsKey(student.Id))
        {
            throw new KeyNotFoundException($"Student with ID '{student.Id}' was not found.");
        }

        _students[student.Id] = student;
    }

    public bool Delete(Guid id)
    {
        return _students.TryRemove(id, out _);
    }
}
