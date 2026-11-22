using TutorMatch.Api.Domain.Users;

namespace TutorMatch.Tests.Common.Fakes;

public class FakeUserRepository : IUserRepository
{
    private readonly Dictionary<Guid, User> _users = new();

    public void Add(User user)
    {
        _users[user.Id] = user;
    }

    public User? GetById(Guid id)
    {
        return _users.TryGetValue(id, out var user) ? user : null;
    }

    public User? GetByEmail(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _users.Values.FirstOrDefault(u => u.Email == normalized);
    }

    public User? GetByGoogleId(string googleId)
    {
        var trimmed = googleId.Trim();
        return _users.Values.FirstOrDefault(u => u.GoogleId == trimmed);
    }

    public IReadOnlyList<User> GetAll()
    {
        return _users.Values.ToList().AsReadOnly();
    }

    public void Update(User user)
    {
        _users[user.Id] = user;
    }

    public bool Delete(Guid id)
    {
        return _users.Remove(id);
    }
}
