using Microsoft.EntityFrameworkCore;
using TutorMatch.Api.Domain.Students;

namespace TutorMatch.Api.Infrastructure.Persistence.Repositories;

// SQLite persistence implementation is infrastructure glue and is excluded from unit-test coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class SqliteStudentRepository : IStudentRepository
{
    private readonly TutorMatchDbContext _context;

    public SqliteStudentRepository(TutorMatchDbContext context)
    {
        _context = context;
    }

    public void Add(Student student)
    {
        ArgumentNullException.ThrowIfNull(student);

        if (student.Id == Guid.Empty)
        {
            throw new ArgumentException("Student ID cannot be empty.", nameof(student));
        }

        if (_context.Students.Any(s => s.Id == student.Id))
        {
            throw new InvalidOperationException($"A student with ID '{student.Id}' already exists.");
        }

        _context.Students.Add(student);
        _context.SaveChanges();
    }

    public Student? GetById(Guid id)
    {
        return _context.Students
            .Include(s => s.Sessions)
            .FirstOrDefault(s => s.Id == id);
    }

    public IReadOnlyList<Student> GetAll()
    {
        return _context.Students
            .Include(s => s.Sessions)
            .ToList()
            .AsReadOnly();
    }

    public void Update(Student student)
    {
        ArgumentNullException.ThrowIfNull(student);

        var existing = _context.Students.Find(student.Id);
        if (existing is null)
        {
            throw new KeyNotFoundException($"Student with ID '{student.Id}' was not found.");
        }

        _context.Entry(existing).CurrentValues.SetValues(student);
        _context.SaveChanges();
    }

    public bool Delete(Guid id)
    {
        var student = _context.Students.Find(id);
        if (student is null)
        {
            return false;
        }

        _context.Students.Remove(student);
        _context.SaveChanges();
        return true;
    }
}
