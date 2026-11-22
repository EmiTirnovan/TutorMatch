using TutorMatch.Api.Domain.Students;

namespace TutorMatch.Tests.Common.Fakes;

public class FakeStudentRepository : IStudentRepository
{
    private readonly Dictionary<Guid, Student> _students = new();

    public void Add(Student student)
    {
        _students[student.Id] = student;
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
        _students[student.Id] = student;
    }

    public bool Delete(Guid id)
    {
        return _students.Remove(id);
    }
}
