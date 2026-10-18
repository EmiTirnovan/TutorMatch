using FluentAssertions;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Domain.Students;
using Xunit;

namespace TutorMatch.Tests.Domain.Students;

public class StudentTests
{
    private static DateRange CreateSampleInterval(int startOffsetHours = 1, int durationHours = 1)
    {
        var start = DateTime.UtcNow.AddHours(startOffsetHours);
        return new DateRange(start, start.AddHours(durationHours));
    }

    [Fact]
    public void Constructor_ValidId_ShouldInstantiateStudentWithEmptySessions()
    {
        var id = Guid.NewGuid();
        var student = new Student(id, "Alex", "alex@example.com");

        student.Id.Should().Be(id);
        student.Name.Should().Be("Alex");
        student.Email.Should().Be("alex@example.com");
        student.Sessions.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_EmptyId_ShouldThrowArgumentException()
    {
        var act = () => new Student(Guid.Empty, "Alex", "alex@example.com");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BookSession_ValidParameters_ShouldCreateSessionAndAddToCollection()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        var student = new Student(studentId, "Alex", "alex@example.com");
        var interval = CreateSampleInterval();

        var session = student.BookSession(tutorId, interval);

        session.Should().NotBeNull();
        session.StudentId.Should().Be(studentId);
        session.TutorId.Should().Be(tutorId);
        session.Interval.Should().Be(interval);
        student.Sessions.Should().ContainSingle().Which.Should().Be(session);
    }

    [Fact]
    public void BookSession_TutorSameAsStudent_ShouldThrowArgumentException()
    {
        var studentId = Guid.NewGuid();
        var student = new Student(studentId, "Alex", "alex@example.com");
        var interval = CreateSampleInterval();

        var act = () => student.BookSession(studentId, interval);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BookSession_OverlappingSession_ShouldThrowInvalidOperationException()
    {
        var student = new Student(Guid.NewGuid(), "Alex", "alex@example.com");
        var tutor1 = Guid.NewGuid();
        var tutor2 = Guid.NewGuid();
        var interval = CreateSampleInterval(1, 2);

        student.BookSession(tutor1, interval);

        var overlappingInterval = CreateSampleInterval(2, 2);
        var act = () => student.BookSession(tutor2, overlappingInterval);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddSession_ValidSession_ShouldAddToCollection()
    {
        var studentId = Guid.NewGuid();
        var student = new Student(studentId, "Alex", "alex@example.com");
        var session = Session.Create(Guid.NewGuid(), studentId, CreateSampleInterval());

        student.AddSession(session);

        student.Sessions.Should().ContainSingle().Which.Should().Be(session);
    }

    [Fact]
    public void AddSession_MismatchStudentId_ShouldThrowInvalidOperationException()
    {
        var student = new Student(Guid.NewGuid(), "Alex", "alex@example.com");
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval());

        var act = () => student.AddSession(session);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddSession_DuplicateSession_ShouldThrowInvalidOperationException()
    {
        var studentId = Guid.NewGuid();
        var student = new Student(studentId, "Alex", "alex@example.com");
        var session = Session.Create(Guid.NewGuid(), studentId, CreateSampleInterval());

        student.AddSession(session);
        var act = () => student.AddSession(session);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CancelSession_ExistingSession_ShouldCancelSession()
    {
        var studentId = Guid.NewGuid();
        var student = new Student(studentId, "Alex", "alex@example.com");
        var session = student.BookSession(Guid.NewGuid(), CreateSampleInterval());

        student.CancelSession(session.Id);

        session.Status.Should().Be(SessionStatus.Cancelled);
    }

    [Fact]
    public void CancelSession_NonExistentSession_ShouldThrowKeyNotFoundException()
    {
        var student = new Student(Guid.NewGuid(), "Alex", "alex@example.com");

        var act = () => student.CancelSession(Guid.NewGuid());

        act.Should().Throw<KeyNotFoundException>();
    }
}