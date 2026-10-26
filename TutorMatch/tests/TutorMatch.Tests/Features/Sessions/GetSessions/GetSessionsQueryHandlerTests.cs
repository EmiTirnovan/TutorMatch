using FluentAssertions;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Features.Sessions.GetSessions;
using Xunit;

namespace TutorMatch.Tests.Features.Sessions.GetSessions;

public class GetSessionsQueryHandlerTests
{
    private sealed class FakeSessionRepository : ISessionRepository
    {
        private readonly List<Session> _sessions = new();

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
    public async Task Given_sessionsExist_When_handlingGetSessions_Then_returnsMappedResponses()
    {
        var sessionOne = Session.Create(Guid.NewGuid(), Guid.NewGuid(), new DateRange(new DateTime(2026, 10, 24, 10, 0, 0), new DateTime(2026, 10, 24, 11, 0, 0)));
        var sessionTwo = Session.Create(Guid.NewGuid(), Guid.NewGuid(), new DateRange(new DateTime(2026, 10, 25, 9, 0, 0), new DateTime(2026, 10, 25, 10, 0, 0)));
        var repository = new FakeSessionRepository();
        repository.Add(sessionOne);
        repository.Add(sessionTwo);
        var handler = new GetSessionsQueryHandler(repository);

        var result = await handler.HandleAsync(new GetSessionsQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Id.Should().Be(sessionOne.Id);
        result.Value[1].Id.Should().Be(sessionTwo.Id);
    }
}
