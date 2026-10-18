namespace TutorMatch.Api.Domain.Sessions;

public interface ISessionRepository
{
    void Add(Session session);
    Session? GetById(Guid id);
    IReadOnlyList<Session> GetAll();
    void Update(Session session);
    bool Delete(Guid id);
}