using FluentAssertions;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Features.Sessions.UpdateSession;
using Xunit;

namespace TutorMatch.Tests.Features.Sessions.UpdateSession;

public class UpdateSessionCommandHandlerTests
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
    public async Task Given_missingSession_When_handlingUpdateSession_Then_returnsNotFoundError()
    {
        var repository = new FakeSessionRepository();
        var handler = new UpdateSessionCommandHandler(repository);

        var result = await handler.HandleAsync(new UpdateSessionCommand(Guid.NewGuid(), new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 11, 0, 0)));

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Given_validSession_When_handlingUpdateSession_Then_returnsSuccess()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), new DateRange(new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 11, 0, 0)));
        var repository = new FakeSessionRepository();
        repository.Add(session);
        var handler = new UpdateSessionCommandHandler(repository);
        var newStart = new DateTime(2026, 10, 21, 12, 0, 0);
        var newEnd = new DateTime(2026, 10, 21, 13, 0, 0);

        var result = await handler.HandleAsync(new UpdateSessionCommand(session.Id, newStart, newEnd));

        result.IsSuccess.Should().BeTrue();
        repository.GetById(session.Id)!.StartTime.Should().Be(newStart);
        repository.GetById(session.Id)!.EndTime.Should().Be(newEnd);
    }

    [Fact]
    public async Task Given_invalidRange_When_handlingUpdateSession_Then_returnsValidationError()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), new DateRange(new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 11, 0, 0)));
        var repository = new FakeSessionRepository();
        repository.Add(session);
        var handler = new UpdateSessionCommandHandler(repository);

        var result = await handler.HandleAsync(new UpdateSessionCommand(session.Id, new DateTime(2026, 10, 20, 11, 0, 0), new DateTime(2026, 10, 20, 10, 0, 0)));

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Given_nonBookedSession_When_handlingUpdateSession_Then_returnsConflictError()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), new DateRange(new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 11, 0, 0)));
        session.Complete();
        var repository = new FakeSessionRepository();
        repository.Add(session);
        var handler = new UpdateSessionCommandHandler(repository);

        var result = await handler.HandleAsync(new UpdateSessionCommand(session.Id, new DateTime(2026, 10, 21, 12, 0, 0), new DateTime(2026, 10, 21, 13, 0, 0)));

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }
}
