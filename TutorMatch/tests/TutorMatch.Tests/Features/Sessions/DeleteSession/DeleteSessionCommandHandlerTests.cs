using FluentAssertions;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Features.Sessions.DeleteSession;
using Xunit;

namespace TutorMatch.Tests.Features.Sessions.DeleteSession;

public class DeleteSessionCommandHandlerTests
{
    private sealed class FakeSessionRepository : ISessionRepository
    {
        private readonly Dictionary<Guid, Session> _sessions = new();

        public void Add(Session session)
        {
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
    public async Task Given_existingSession_When_handlingDeleteSession_Then_returnsSuccess()
    {
        var sessionId = Guid.NewGuid();
        var repository = new FakeSessionRepository();
        var handler = new DeleteSessionCommandHandler(repository);
        repository.Add(Session.Create(Guid.NewGuid(), Guid.NewGuid(), new DateRange(new DateTime(2026, 10, 21, 10, 0, 0), new DateTime(2026, 10, 21, 11, 0, 0))));

        var session = new Session(
            sessionId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateRange(new DateTime(2026, 10, 22, 10, 0, 0), new DateTime(2026, 10, 22, 11, 0, 0)));
        repository.Add(session);

        var result = await handler.HandleAsync(new DeleteSessionCommand(sessionId));

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public async Task Given_missingSession_When_handlingDeleteSession_Then_returnsNotFoundError()
    {
        var repository = new FakeSessionRepository();
        var handler = new DeleteSessionCommandHandler(repository);

        var result = await handler.HandleAsync(new DeleteSessionCommand(Guid.NewGuid()));

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }
}
