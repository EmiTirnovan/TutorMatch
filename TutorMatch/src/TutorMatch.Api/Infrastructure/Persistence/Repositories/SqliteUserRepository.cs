using Microsoft.EntityFrameworkCore;
using TutorMatch.Api.Domain.Users;

namespace TutorMatch.Api.Infrastructure.Persistence.Repositories;

public class SqliteUserRepository : IUserRepository
{
    private readonly TutorMatchDbContext _dbContext;

    public SqliteUserRepository(TutorMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(User user)
    {
        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();
    }

    public User? GetById(Guid id)
    {
        return _dbContext.Users.Find(id);
    }

    public User? GetByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return _dbContext.Users.FirstOrDefault(u => u.Email == normalizedEmail);
    }

    public User? GetByGoogleId(string googleId)
    {
        if (string.IsNullOrWhiteSpace(googleId)) return null;
        var trimmedGoogleId = googleId.Trim();
        return _dbContext.Users.FirstOrDefault(u => u.GoogleId == trimmedGoogleId);
    }

    public IReadOnlyList<User> GetAll()
    {
        return _dbContext.Users.AsNoTracking().ToList().AsReadOnly();
    }

    public void Update(User user)
    {
        _dbContext.Users.Update(user);
        _dbContext.SaveChanges();
    }

    public bool Delete(Guid id)
    {
        var user = _dbContext.Users.Find(id);
        if (user is null)
        {
            return false;
        }

        _dbContext.Users.Remove(user);
        _dbContext.SaveChanges();
        return true;
    }
}
