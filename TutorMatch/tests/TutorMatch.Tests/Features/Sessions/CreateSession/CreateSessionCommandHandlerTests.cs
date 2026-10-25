using FluentAssertions;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Features.Sessions.CreateSession;
using Xunit;

namespace TutorMatch.Tests.Features.Sessions.CreateSession;

public class CreateSessionCommandHandlerTests
{
    private sealed class FakeSessionRepository : ISessionRepository
    {
        public Action<Session>? AddBehavior { get; set; }
        private readonly Dictionary<Guid, Session> _sessions = new();

        public void Add(Session session)
        {
            AddBehavior?.Invoke(session);
            _sessions[session.Id] = session;
        }

        public Session? GetById(Guid id)
        {
            return _sessions.TryGetValue(id, out var session) ? session : null;
        }

        public IReadOnlyList<Session> GetAll()
        {
            return _sessions.Values.ToList().AsReadOnly();
        }

        public void Update(Session session)
        {
            _sessions[session.Id] = session;
        }

        public bool Delete(Guid id)
        {
            return _sessions.Remove(id);
        }
    }

    [Fact]
    public async Task Given_validCommand_When_handlingCreateSession_Then_returnsSuccessfulResult()
    {
        var repository = new FakeSessionRepository();
        var handler = new CreateSessionCommandHandler(repository);
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var startTime = new DateTime(2026, 10, 20, 10, 0, 0);
        var endTime = new DateTime(2026, 10, 20, 11, 0, 0);
        var command = new CreateSessionCommand(tutorId, studentId, startTime, endTime);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.TutorId.Should().Be(tutorId);
        result.Value.StudentId.Should().Be(studentId);
        result.Value.StartTime.Should().Be(startTime);
        result.Value.EndTime.Should().Be(endTime);
        result.Value.Status.Should().Be(SessionStatus.Booked);
    }

    [Fact]
    public async Task Given_emptyIds_When_handlingCreateSession_Then_returnsValidationError()
    {
        var repository = new FakeSessionRepository();
        var handler = new CreateSessionCommandHandler(repository);
        var command = new CreateSessionCommand(Guid.Empty, Guid.NewGuid(), new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 11, 0, 0));

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Session.Validation");
    }

    [Fact]
    public async Task Given_invalidRange_When_handlingCreateSession_Then_returnsValidationError()
    {
        var repository = new FakeSessionRepository();
        var handler = new CreateSessionCommandHandler(repository);
        var command = new CreateSessionCommand(Guid.NewGuid(), Guid.NewGuid(), new DateTime(2026, 10, 20, 11, 0, 0), new DateTime(2026, 10, 20, 10, 0, 0));

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Session.Validation");
    }

    [Fact]
    public async Task Given_repositoryConflict_When_handlingCreateSession_Then_returnsConflictError()
    {
        var repository = new FakeSessionRepository
        {
            AddBehavior = session => throw new InvalidOperationException("duplicate")
        };
        var handler = new CreateSessionCommandHandler(repository);
        var command = new CreateSessionCommand(Guid.NewGuid(), Guid.NewGuid(), new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 11, 0, 0));

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Description.Should().Contain("duplicate");
    }
}
