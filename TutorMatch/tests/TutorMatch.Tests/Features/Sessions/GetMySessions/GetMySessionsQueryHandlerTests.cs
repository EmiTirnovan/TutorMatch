using FluentAssertions;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Features.Sessions.GetMySessions;
using Xunit;

namespace TutorMatch.Tests.Features.Sessions.GetMySessions;

public class GetMySessionsQueryHandlerTests
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
    public async Task Given_emptyUserId_When_handlingGetMySessions_Then_returnsValidationError()
    {
        var repository = new FakeSessionRepository();
        var handler = new GetMySessionsQueryHandler(repository);
        var query = new GetMySessionsQuery(Guid.Empty, "Student");

        var result = await handler.HandleAsync(query);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Session.Validation");
        result.Error.Description.Should().Be("UserId cannot be empty.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Given_nullOrWhitespaceRole_When_handlingGetMySessions_Then_returnsValidationError(string? invalidRole)
    {
        var repository = new FakeSessionRepository();
        var handler = new GetMySessionsQueryHandler(repository);
        var query = new GetMySessionsQuery(Guid.NewGuid(), invalidRole!);

        var result = await handler.HandleAsync(query);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Session.Validation");
        result.Error.Description.Should().Be("Role must be either 'Student' or 'Tutor'.");
    }

    [Fact]
    public async Task Given_unrecognizedRole_When_handlingGetMySessions_Then_returnsValidationError()
    {
        var repository = new FakeSessionRepository();
        var handler = new GetMySessionsQueryHandler(repository);
        var query = new GetMySessionsQuery(Guid.NewGuid(), "Admin");

        var result = await handler.HandleAsync(query);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Session.Validation");
        result.Error.Description.Should().Be("Role must be either 'Student' or 'Tutor'.");
    }

    [Fact]
    public async Task Given_studentRoleWithoutStatusFilter_When_handlingGetMySessions_Then_returnsAllStudentSessions()
    {
        var studentId = Guid.NewGuid();
        var otherStudentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        var session1 = new Session(Guid.NewGuid(), tutorId, studentId, new DateRange(new DateTime(2026, 11, 4, 10, 0, 0), new DateTime(2026, 11, 4, 11, 0, 0)));
        var session2 = new Session(Guid.NewGuid(), tutorId, studentId, new DateRange(new DateTime(2026, 11, 3, 10, 0, 0), new DateTime(2026, 11, 3, 11, 0, 0)));
        var otherSession = new Session(Guid.NewGuid(), tutorId, otherStudentId, new DateRange(new DateTime(2026, 11, 5, 10, 0, 0), new DateTime(2026, 11, 5, 11, 0, 0)));
        var repository = new FakeSessionRepository();
        repository.Add(session1);
        repository.Add(session2);
        repository.Add(otherSession);
        var handler = new GetMySessionsQueryHandler(repository);
        var query = new GetMySessionsQuery(studentId, "student");

        var result = await handler.HandleAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Sessions.Should().HaveCount(2);
        result.Value.Sessions[0].Id.Should().Be(session2.Id);
        result.Value.Sessions[1].Id.Should().Be(session1.Id);
    }

    [Fact]
    public async Task Given_tutorRoleWithoutStatusFilter_When_handlingGetMySessions_Then_returnsAllTutorSessions()
    {
        var tutorId = Guid.NewGuid();
        var otherTutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var session1 = new Session(Guid.NewGuid(), tutorId, studentId, new DateRange(new DateTime(2026, 11, 3, 10, 0, 0), new DateTime(2026, 11, 3, 11, 0, 0)));
        var otherSession = new Session(Guid.NewGuid(), otherTutorId, studentId, new DateRange(new DateTime(2026, 11, 4, 10, 0, 0), new DateTime(2026, 11, 4, 11, 0, 0)));
        var repository = new FakeSessionRepository();
        repository.Add(session1);
        repository.Add(otherSession);
        var handler = new GetMySessionsQueryHandler(repository);
        var query = new GetMySessionsQuery(tutorId, "Tutor");

        var result = await handler.HandleAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Sessions.Should().HaveCount(1);
        result.Value.Sessions[0].Id.Should().Be(session1.Id);
    }

    [Fact]
    public async Task Given_studentRoleWithStatusFilter_When_handlingGetMySessions_Then_returnsFilteredSessions()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        var bookedSession = new Session(Guid.NewGuid(), tutorId, studentId, new DateRange(new DateTime(2026, 11, 3, 10, 0, 0), new DateTime(2026, 11, 3, 11, 0, 0)));
        var cancelledSession = new Session(Guid.NewGuid(), tutorId, studentId, new DateRange(new DateTime(2026, 11, 4, 10, 0, 0), new DateTime(2026, 11, 4, 11, 0, 0)));
        cancelledSession.Cancel();
        var repository = new FakeSessionRepository();
        repository.Add(bookedSession);
        repository.Add(cancelledSession);
        var handler = new GetMySessionsQueryHandler(repository);
        var query = new GetMySessionsQuery(studentId, "Student", SessionStatus.Cancelled);

        var result = await handler.HandleAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Sessions.Should().HaveCount(1);
        result.Value.Sessions[0].Id.Should().Be(cancelledSession.Id);
        result.Value.Sessions[0].Status.Should().Be(SessionStatus.Cancelled);
    }

    [Fact]
    public async Task Given_noMatchingSessions_When_handlingGetMySessions_Then_returnsEmptyList()
    {
        var repository = new FakeSessionRepository();
        var handler = new GetMySessionsQueryHandler(repository);
        var query = new GetMySessionsQuery(Guid.NewGuid(), "Student");

        var result = await handler.HandleAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Sessions.Should().BeEmpty();
    }
}

