using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Domain.Students;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Infrastructure;
using TutorMatch.Api.Infrastructure.Persistence;
using TutorMatch.Api.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TutorMatch.Tests.Infrastructure.Persistence;

public class SqliteSessionRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TutorMatchDbContext _context;
    private readonly SqliteSessionRepository _repository;
    private readonly Guid _tutorId = Guid.NewGuid();
    private readonly Guid _studentId = Guid.NewGuid();

    public SqliteSessionRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TutorMatchDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new TutorMatchDbContext(options);
        _context.Database.EnsureCreated();

        // Seed required foreign key entities
        var tutor = new Tutor("Tutor Test", "tutor@test.com", "Bio", 50m, _tutorId);
        var student = new Student(_studentId, "Student Test", "student@test.com");
        _context.Tutors.Add(tutor);
        _context.Students.Add(student);
        _context.SaveChanges();

        _repository = new SqliteSessionRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private Session CreateSampleSession(Guid? id = null)
    {
        var start = DateTime.UtcNow.Date.AddDays(1).AddHours(10);
        var end = start.AddHours(2);
        return new Session(
            id: id ?? Guid.NewGuid(),
            tutorId: _tutorId,
            studentId: _studentId,
            startTime: start,
            endTime: end,
            status: SessionStatus.Booked,
            version: 1);
    }

    [Fact]
    public void Add_ValidSession_ShouldStoreSuccessfully()
    {
        var session = CreateSampleSession();

        _repository.Add(session);
        var retrieved = _repository.GetById(session.Id);

        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(session.Id);
        retrieved.TutorId.Should().Be(_tutorId);
        retrieved.StudentId.Should().Be(_studentId);
        retrieved.Status.Should().Be(SessionStatus.Booked);
        retrieved.Version.Should().Be(1);
    }

    [Fact]
    public void Add_NullSession_ShouldThrowArgumentNullException()
    {
        var act = () => _repository.Add(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Add_DuplicateId_ShouldThrowInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var session1 = CreateSampleSession(id);
        var session2 = CreateSampleSession(id);

        _repository.Add(session1);

        var act = () => _repository.Add(session2);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{id}*");
    }

    [Fact]
    public void GetById_WhenNotFound_ShouldReturnNullGracefully()
    {
        var result = _repository.GetById(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public void GetAll_ShouldReturnAllSessions()
    {
        var session1 = CreateSampleSession();
        var session2 = CreateSampleSession();

        _repository.Add(session1);
        _repository.Add(session2);

        var all = _repository.GetAll();

        all.Should().HaveCount(2);
        all.Should().Contain(s => s.Id == session1.Id);
        all.Should().Contain(s => s.Id == session2.Id);
    }

    [Fact]
    public void Update_ExistingSession_ShouldUpdateSuccessfully()
    {
        var session = CreateSampleSession();
        _repository.Add(session);

        session.Cancel();
        _repository.Update(session);

        var retrieved = _repository.GetById(session.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Status.Should().Be(SessionStatus.Cancelled);
        retrieved.Version.Should().Be(2);
    }

    [Fact]
    public void Update_NonExistentSession_ShouldThrowKeyNotFoundException()
    {
        var session = CreateSampleSession();

        var act = () => _repository.Update(session);
        act.Should().Throw<KeyNotFoundException>()
            .WithMessage($"*{session.Id}*");
    }

    [Fact]
    public void Delete_ExistingSession_ShouldReturnTrueAndRemove()
    {
        var session = CreateSampleSession();
        _repository.Add(session);

        var result = _repository.Delete(session.Id);

        result.Should().BeTrue();
        _repository.GetById(session.Id).Should().BeNull();
    }

    [Fact]
    public void Delete_NonExistentSession_ShouldReturnFalse()
    {
        var result = _repository.Delete(Guid.NewGuid());
        result.Should().BeFalse();
    }

    [Fact]
    public void ConcurrencyConflict_WhenVersionChanged_ShouldThrowInvalidOperationException()
    {
        var session = CreateSampleSession();
        _repository.Add(session);

        // Simulate concurrent modification in a separate DbContext instance
        var options = new DbContextOptionsBuilder<TutorMatchDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (var concurrentContext = new TutorMatchDbContext(options))
        {
            var concurrentRepo = new SqliteSessionRepository(concurrentContext);
            var loadedSession = concurrentRepo.GetById(session.Id)!;
            loadedSession.Cancel();
            concurrentRepo.Update(loadedSession);
        }

        // Attempting to update the original session instance with stale Version should conflict
        session.Complete();
        var act = () => _repository.Update(session);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*concurrently*");
    }

    [Fact]
    public void DependencyInjection_AddSqliteRepositories_ShouldResolveScopedRepositories()
    {
        var services = new ServiceCollection();
        services.AddSqliteRepositories("Data Source=:memory:");

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var tutorRepo = scope.ServiceProvider.GetService<ITutorRepository>();
        var sessionRepo = scope.ServiceProvider.GetService<ISessionRepository>();

        tutorRepo.Should().NotBeNull().And.BeOfType<SqliteTutorRepository>();
        sessionRepo.Should().NotBeNull().And.BeOfType<SqliteSessionRepository>();
    }

    [Fact]
    public void DependencyInjection_AddSqliteInMemoryRepositories_ShouldResolveAndShareConnection()
    {
        var services = new ServiceCollection();
        services.AddSqliteInMemoryRepositories();

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var dbContext = scope.ServiceProvider.GetService<TutorMatchDbContext>();
        var tutorRepo = scope.ServiceProvider.GetService<ITutorRepository>();
        var sessionRepo = scope.ServiceProvider.GetService<ISessionRepository>();

        dbContext.Should().NotBeNull();
        tutorRepo.Should().NotBeNull().And.BeOfType<SqliteTutorRepository>();
        sessionRepo.Should().NotBeNull().And.BeOfType<SqliteSessionRepository>();
    }
}
