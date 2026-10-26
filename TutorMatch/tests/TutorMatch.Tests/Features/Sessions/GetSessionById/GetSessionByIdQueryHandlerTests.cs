using FluentAssertions;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Features.Sessions.GetSessionById;
using Xunit;

namespace TutorMatch.Tests.Features.Sessions.GetSessionById;

public class GetSessionByIdQueryHandlerTests
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
    public async Task Given_existingSession_When_handlingGetSessionById_Then_returnsSessionResponse()
    {
        var sessionId = Guid.NewGuid();
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), new DateRange(new DateTime(2026, 10, 23, 10, 0, 0), new DateTime(2026, 10, 23, 11, 0, 0)));
        var repository = new FakeSessionRepository();
        repository.Add(session);
        var handler = new GetSessionByIdQueryHandler(repository);

        var result = await handler.HandleAsync(new GetSessionByIdQuery(session.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(session.Id);
        result.Value.TutorId.Should().Be(session.TutorId);
        result.Value.StudentId.Should().Be(session.StudentId);
        result.Value.Status.Should().Be(SessionStatus.Booked);
    }

    [Fact]
    public async Task Given_missingSession_When_handlingGetSessionById_Then_returnsNotFoundError()
    {
        var repository = new FakeSessionRepository();
        var handler = new GetSessionByIdQueryHandler(repository);

        var result = await handler.HandleAsync(new GetSessionByIdQuery(Guid.NewGuid()));

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }
}
