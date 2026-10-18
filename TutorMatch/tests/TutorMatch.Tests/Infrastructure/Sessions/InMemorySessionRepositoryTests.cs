using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Infrastructure;
using TutorMatch.Api.Infrastructure.Sessions;
using Xunit;

namespace TutorMatch.Tests.Infrastructure.Sessions;

public class InMemorySessionRepositoryTests
{
    private readonly InMemorySessionRepository _repository = new();

    private static Session CreateSampleSession(Guid? id = null, Guid? tutorId = null, Guid? studentId = null)
    {
        return new Session(
            id: id ?? Guid.NewGuid(),
            tutorId: tutorId ?? Guid.NewGuid(),
            studentId: studentId ?? Guid.NewGuid(),
            startTime: DateTime.UtcNow.AddHours(1),
            endTime: DateTime.UtcNow.AddHours(2),
            status: SessionStatus.Booked,
            version: 1);
    }

    [Fact]
    public void Add_ValidSession_ShouldStoreSessionSuccessfully()
    {
        // Arrange
        var session = CreateSampleSession();

        // Act
        _repository.Add(session);
        var retrieved = _repository.GetById(session.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(session.Id);
        retrieved.TutorId.Should().Be(session.TutorId);
        retrieved.StudentId.Should().Be(session.StudentId);
    }

    [Fact]
    public void Add_DuplicateId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var session1 = CreateSampleSession(id: id);
        var session2 = CreateSampleSession(id: id);

        _repository.Add(session1);

        // Act & Assert
        var act = () => _repository.Add(session2);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{id}*");
    }

    [Fact]
    public void Add_NullSession_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => _repository.Add(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Add_EmptyGuidSession_ShouldThrowArgumentException()
    {
        // Arrange
        var session = CreateSampleSession(id: Guid.Empty);

        // Act & Assert
        var act = () => _repository.Add(session);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetById_NonExistentSession_ShouldReturnNullSafelyWithoutException()
    {
        // Act
        var result = _repository.GetById(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetAll_WhenEmpty_ShouldReturnEmptyReadOnlyList()
    {
        // Act
        var allSessions = _repository.GetAll();

        // Assert
        allSessions.Should().NotBeNull();
        allSessions.Should().BeEmpty();
    }

    [Fact]
    public void GetAll_ShouldReturnSnapshotIsolatedFromSubsequentAdditions()
    {
        // Arrange
        var session1 = CreateSampleSession();
        var session2 = CreateSampleSession();
        _repository.Add(session1);

        // Act
        var snapshot = _repository.GetAll();
        _repository.Add(session2);

        // Assert
        snapshot.Should().HaveCount(1);
        snapshot.Should().Contain(s => s.Id == session1.Id);
        snapshot.Should().NotContain(s => s.Id == session2.Id);

        var currentAll = _repository.GetAll();
        currentAll.Should().HaveCount(2);
    }

    [Fact]
    public void Update_ExistingSession_ShouldUpdateSessionSuccessfully()
    {
        // Arrange
        var session = CreateSampleSession();
        _repository.Add(session);

        var updatedSession = new Session(
            id: session.Id,
            tutorId: session.TutorId,
            studentId: session.StudentId,
            startTime: session.StartTime,
            endTime: session.EndTime,
            status: SessionStatus.Completed,
            version: 2);

        // Act
        _repository.Update(updatedSession);
        var retrieved = _repository.GetById(session.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Status.Should().Be(SessionStatus.Completed);
        retrieved.Version.Should().Be(2);
    }

    [Fact]
    public void Update_NonExistentSession_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var nonExistent = CreateSampleSession();

        // Act & Assert
        var act = () => _repository.Update(nonExistent);
        act.Should().Throw<KeyNotFoundException>()
            .WithMessage($"*{nonExistent.Id}*");
    }

    [Fact]
    public void Update_NullSession_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => _repository.Update(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Delete_ExistingSession_ShouldReturnTrueAndRemoveEntity()
    {
        // Arrange
        var session = CreateSampleSession();
        _repository.Add(session);

        // Act
        var deleted = _repository.Delete(session.Id);
        var retrieved = _repository.GetById(session.Id);

        // Assert
        deleted.Should().BeTrue();
        retrieved.Should().BeNull();
    }

    [Fact]
    public void Delete_NonExistentSession_ShouldReturnFalseSafelyWithoutException()
    {
        // Act
        var deleted = _repository.Delete(Guid.NewGuid());

        // Assert
        deleted.Should().BeFalse();
    }

    [Fact]
    public void DependencyInjection_ShouldRegisterISessionRepositoryAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSessionInfrastructure();
        var provider = services.BuildServiceProvider();

        // Act
        var instance1 = provider.GetService<ISessionRepository>();
        var instance2 = provider.GetService<ISessionRepository>();

        // Assert
        instance1.Should().NotBeNull();
        instance1.Should().BeOfType<InMemorySessionRepository>();
        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public async Task ConcurrentAccess_AddingDistinctSessions_ShouldBeThreadSafe()
    {
        // Arrange
        const int count = 200;
        var sessions = Enumerable.Range(0, count)
            .Select(_ => CreateSampleSession())
            .ToList();

        // Act
        var tasks = sessions.Select(s => Task.Run(() => _repository.Add(s)));
        await Task.WhenAll(tasks);

        // Assert
        var all = _repository.GetAll();
        all.Should().HaveCount(count);

        foreach (var s in sessions)
        {
            _repository.GetById(s.Id).Should().NotBeNull();
        }
    }

    [Fact]
    public async Task ConcurrentAccess_AddingSameIdSimultaneously_ShouldAllowOnlyOneWinner()
    {
        // Arrange
        var sharedId = Guid.NewGuid();
        const int attempts = 50;
        var successes = 0;
        var duplicateExceptions = 0;

        // Act
        var tasks = Enumerable.Range(0, attempts).Select(_ => Task.Run(() =>
        {
            var session = CreateSampleSession(id: sharedId);
            try
            {
                _repository.Add(session);
                Interlocked.Increment(ref successes);
            }
            catch (InvalidOperationException)
            {
                Interlocked.Increment(ref duplicateExceptions);
            }
        }));

        await Task.WhenAll(tasks);

        // Assert
        successes.Should().Be(1);
        duplicateExceptions.Should().Be(attempts - 1);
        _repository.GetById(sharedId).Should().NotBeNull();
    }

    [Fact]
    public async Task ConcurrentAccess_MixedOperations_ShouldMaintainConsistency()
    {
        // Arrange
        const int initialCount = 100;
        var initialSessions = Enumerable.Range(0, initialCount)
            .Select(_ => CreateSampleSession())
            .ToList();

        foreach (var s in initialSessions)
        {
            _repository.Add(s);
        }

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var random = new Random();

        // Act
        var readTask = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var snapshot = _repository.GetAll();
                // Enumerating snapshot while other threads modify repository should not throw
                foreach (var item in snapshot)
                {
                    _ = item.Id;
                }
            }
        });

        var writeTask = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var newSession = CreateSampleSession();
                _repository.Add(newSession);

                _ = _repository.Delete(newSession.Id);
            }
        });

        await Task.WhenAll(readTask, writeTask);

        // Assert
        _repository.GetAll().Should().NotBeNull();
    }
}