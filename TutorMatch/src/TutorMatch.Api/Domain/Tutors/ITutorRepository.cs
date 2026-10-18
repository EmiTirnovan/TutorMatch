namespace TutorMatch.Api.Domain.Tutors;

// Interfata pentru salvarea si interogarea tutorilor, independenta de baza de date
public interface ITutorRepository
{
    void Add(Tutor tutor);
    Tutor? GetById(Guid id);
    IReadOnlyList<Tutor> GetAll();
    void Update(Tutor tutor);
    bool Delete(Guid id);
}
