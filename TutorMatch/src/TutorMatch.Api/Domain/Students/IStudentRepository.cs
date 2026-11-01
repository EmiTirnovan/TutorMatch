namespace TutorMatch.Api.Domain.Students;

public interface IStudentRepository
{
    void Add(Student student);
    Student? GetById(Guid id);
    IReadOnlyList<Student> GetAll();
    void Update(Student student);
    bool Delete(Guid id);
}
