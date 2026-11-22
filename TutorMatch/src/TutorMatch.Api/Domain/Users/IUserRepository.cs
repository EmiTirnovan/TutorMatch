namespace TutorMatch.Api.Domain.Users;

public interface IUserRepository
{
    void Add(User user);
    User? GetById(Guid id);
    User? GetByEmail(string email);
    User? GetByGoogleId(string googleId);
    IReadOnlyList<User> GetAll();
    void Update(User user);
    bool Delete(Guid id);
}
