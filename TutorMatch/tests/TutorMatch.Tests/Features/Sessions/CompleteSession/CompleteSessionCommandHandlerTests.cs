using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Features.Sessions.CompleteSession;
using Xunit;

namespace TutorMatch.Tests.Features.Sessions.CompleteSession;

public class CompleteSessionCommandHandlerTests
{
    private sealed class FakeSessionRepository : ISessionRepository
    {
        private readonly List<Session> _sessions = new();
        public bool ThrowArgumentExceptionOnUpdate { get; set; }

        public void Add(Session session)
        {
            _sessions.Add(session);
        }

        public Session? GetById(Guid id)
        {
            return _sessions.FirstOrDefault(s => s.Id == id);
        }

        public IReadOnlyList<Session> GetAll()
        {
            return _sessions.AsReadOnly();
        }

        public void Update(Session session)
        {
            if (ThrowArgumentExceptionOnUpdate)
            {
                throw new ArgumentException("Invalid session state for update.");
            }

            var index = _sessions.FindIndex(s => s.Id == session.Id);
            if (index >= 0)
            {
                _sessions[index] = session;
            }
        }

        public bool Delete(Guid id)
        {
            var session = _sessions.FirstOrDefault(s => s.Id == id);
            if (session is null)
            {
                return false;
            }

            _sessions.Remove(session);
            return true;
        }
    }

    [Fact]
    public async Task Given_emptySessionId_When_handlingCompleteSession_Then_returnsValidationError()
    {
        var repository = new FakeSessionRepository();
        var fakeClock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var handler = new CompleteSessionCommandHandler(repository, fakeClock);
        var command = new CompleteSessionCommand(Guid.Empty, Guid.NewGuid());

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Session.Validation");
        result.Error.Description.Should().Be("SessionId cannot be empty.");
    }

    [Fact]
    public async Task Given_emptyTutorId_When_handlingCompleteSession_Then_returnsValidationError()
    {
        var repository = new FakeSessionRepository();
        var fakeClock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var handler = new CompleteSessionCommandHandler(repository, fakeClock);
        var command = new CompleteSessionCommand(Guid.NewGuid(), Guid.Empty);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Session.Validation");
        result.Error.Description.Should().Be("TutorId cannot be empty.");
    }

    [Fact]
    public async Task Given_nonExistentSession_When_handlingCompleteSession_Then_returnsNotFoundError()
    {
        var sessionId = Guid.NewGuid();
        var repository = new FakeSessionRepository();
        var fakeClock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var handler = new CompleteSessionCommandHandler(repository, fakeClock);
        var command = new CompleteSessionCommand(sessionId, Guid.NewGuid());

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("Session.NotFound");
        result.Error.Description.Should().Be($"Session with ID '{sessionId}' was not found.");
    }

    [Fact]
    public async Task Given_differentTutorId_When_handlingCompleteSession_Then_returnsConflictError()
    {
        var sessionId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var wrongTutorId = Guid.NewGuid();
        var start = DateTime.UtcNow.AddDays(1);
        var session = new Session(sessionId, tutorId, studentId, new DateRange(start, start.AddHours(1)));
        var repository = new FakeSessionRepository();
        repository.Add(session);
        var fakeClock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var handler = new CompleteSessionCommandHandler(repository, fakeClock);
        var command = new CompleteSessionCommand(sessionId, wrongTutorId);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("Session.Conflict");
        result.Error.Description.Should().Be("Only the assigned tutor can mark this session as completed.");
    }

    [Fact]
    public async Task Given_sessionBeforeStartTime_When_handlingCompleteSession_Then_returnsConflictError()
    {
        var sessionId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var start = new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc);
        var session = new Session(sessionId, tutorId, studentId, new DateRange(start, start.AddHours(2)));
        var repository = new FakeSessionRepository();
        repository.Add(session);
        var fakeClock = new FakeTimeProvider(start.AddMinutes(-10));
        var handler = new CompleteSessionCommandHandler(repository, fakeClock);
        var command = new CompleteSessionCommand(sessionId, tutorId);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("Session.Conflict");
        result.Error.Description.Should().Contain("before its start time");
    }

    [Fact]
    public async Task Given_sessionDuringOrAfterInterval_When_handlingCompleteSession_Then_completesSessionSuccessfully()
    {
        var sessionId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var start = new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc);
        var session = new Session(sessionId, tutorId, studentId, new DateRange(start, start.AddHours(2)));
        var repository = new FakeSessionRepository();
        repository.Add(session);
        var fakeClock = new FakeTimeProvider(start.AddHours(1));
        var handler = new CompleteSessionCommandHandler(repository, fakeClock);
        var command = new CompleteSessionCommand(sessionId, tutorId);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.Completed);
        session.Version.Should().Be(2);
    }

    [Fact]
    public async Task Given_alreadyCancelledSession_When_handlingCompleteSession_Then_returnsConflictError()
    {
        var sessionId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var start = new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc);
        var session = new Session(sessionId, tutorId, studentId, new DateRange(start, start.AddHours(2)));
        session.Cancel();
        var repository = new FakeSessionRepository();
        repository.Add(session);
        var fakeClock = new FakeTimeProvider(start.AddHours(3));
        var handler = new CompleteSessionCommandHandler(repository, fakeClock);
        var command = new CompleteSessionCommand(sessionId, tutorId);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("Session.Conflict");
    }

    [Fact]
    public async Task Given_repositoryThrowsArgumentException_When_handlingCompleteSession_Then_returnsValidationError()
    {
        var sessionId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var start = new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc);
        var session = new Session(sessionId, tutorId, studentId, new DateRange(start, start.AddHours(2)));
        var repository = new FakeSessionRepository { ThrowArgumentExceptionOnUpdate = true };
        repository.Add(session);
        var fakeClock = new FakeTimeProvider(start.AddHours(1));
        var handler = new CompleteSessionCommandHandler(repository, fakeClock);
        var command = new CompleteSessionCommand(sessionId, tutorId);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Session.Validation");
        result.Error.Description.Should().Be("Invalid session state for update.");
    }
}

