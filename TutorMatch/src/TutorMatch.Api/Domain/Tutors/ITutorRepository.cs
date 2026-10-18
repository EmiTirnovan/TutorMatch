namespace TutorMatch.Api.Domain.Tutors;

public interface ITutorRepository
{
    void Add(Tutor tutor);
    Tutor? GetById(Guid id);
    IReadOnlyList<Tutor> GetAll();
    void Update(Tutor tutor);
    bool Delete(Guid id);
}