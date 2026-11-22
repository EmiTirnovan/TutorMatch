using System.Data.Common;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TutorMatch.Api.Domain.Users;
using TutorMatch.Api.Infrastructure.Persistence;
using TutorMatch.Api.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TutorMatch.Tests.Infrastructure.Persistence;

public class SqliteUserRepositoryTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly TutorMatchDbContext _dbContext;
    private readonly SqliteUserRepository _repository;

    public SqliteUserRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TutorMatchDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new TutorMatchDbContext(options);
        _dbContext.Database.EnsureCreated();

        _repository = new SqliteUserRepository(_dbContext);
    }

    [Fact]
    public void Given_User_When_AddingAndQuerying_Then_PersistsAndRetrievesCorrectly()
    {
        var user = new User(
            Guid.NewGuid(),
            "repo.test@tutormatch.ro",
            "hashedPassword",
            "Repo Test User",
            "Student",
            "google-repo-1");

        _repository.Add(user);

        var byId = _repository.GetById(user.Id);
        byId.Should().NotBeNull();
        byId!.Email.Should().Be("repo.test@tutormatch.ro");

        var byEmail = _repository.GetByEmail("repo.test@tutormatch.ro");
        byEmail.Should().NotBeNull();
        byEmail!.Id.Should().Be(user.Id);

        var byGoogleId = _repository.GetByGoogleId("google-repo-1");
        byGoogleId.Should().NotBeNull();
        byGoogleId!.Id.Should().Be(user.Id);

        var all = _repository.GetAll();
        all.Should().Contain(u => u.Id == user.Id);
    }

    [Fact]
    public void Given_ExistingUser_When_Updating_Then_ChangesAreSaved()
    {
        var user = new User(Guid.NewGuid(), "update@tutormatch.ro", "hash", "Old Name", "Student");
        _repository.Add(user);

        user.UpdateProfile("New Name");
        user.SetGoogleId("google-new-id");
        _repository.Update(user);

        var updated = _repository.GetById(user.Id);
        updated!.FullName.Should().Be("New Name");
        updated.GoogleId.Should().Be("google-new-id");
    }

    [Fact]
    public void Given_ExistingUser_When_Deleting_Then_UserIsRemoved()
    {
        var user = new User(Guid.NewGuid(), "delete@tutormatch.ro", "hash", "Delete User", "Student");
        _repository.Add(user);

        var deleted = _repository.Delete(user.Id);
        deleted.Should().BeTrue();

        _repository.GetById(user.Id).Should().BeNull();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
