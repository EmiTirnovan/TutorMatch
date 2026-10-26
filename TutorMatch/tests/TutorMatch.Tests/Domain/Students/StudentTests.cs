using System.Reflection;
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
    public void Given_validData_When_constructingStudent_Then_initializesProperties()
    {
        var id = Guid.NewGuid();
        var student = new Student(id, "Alex", "alex@example.com");

        student.Id.Should().Be(id);
        student.Name.Should().Be("Alex");
        student.Email.Should().Be("alex@example.com");
        student.Sessions.Should().BeEmpty();
    }

    [Fact]
    public void Given_emptyId_When_constructingStudent_Then_throwsArgumentException()
    {
        var action = () => new Student(Guid.Empty, "Alex", "alex@example.com");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Given_validParameters_When_bookingSession_Then_returnsSessionAndAddsToCollection()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        var student = new Student(studentId, "Alex", "alex@example.com");
        var interval = CreateSampleInterval();

        var session = student.BookSession(tutorId, interval);

        session.StudentId.Should().Be(studentId);
        session.TutorId.Should().Be(tutorId);
        session.Interval.Should().Be(interval);
        student.Sessions.Should().ContainSingle().Which.Should().Be(session);
    }

    [Fact]
    public void Given_overlappingInterval_When_bookingSession_Then_throwsInvalidOperationException()
    {
        var student = new Student(Guid.NewGuid(), "Alex", "alex@example.com");
        var firstTutorId = Guid.NewGuid();
        var secondTutorId = Guid.NewGuid();
        var interval = CreateSampleInterval(1, 2);
        student.BookSession(firstTutorId, interval);
        var overlappingInterval = CreateSampleInterval(2, 2);

        var action = () => student.BookSession(secondTutorId, overlappingInterval);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Given_validSession_When_addingSession_Then_addsToCollection()
    {
        var studentId = Guid.NewGuid();
        var student = new Student(studentId, "Alex", "alex@example.com");
        var session = Session.Create(Guid.NewGuid(), studentId, CreateSampleInterval());

        student.AddSession(session);

        student.Sessions.Should().ContainSingle().Which.Should().Be(session);
    }

    [Fact]
    public void Given_missingSession_When_cancellingSession_Then_throwsKeyNotFoundException()
    {
        var student = new Student(Guid.NewGuid(), "Alex", "alex@example.com");

        var action = () => student.CancelSession(Guid.NewGuid());

        action.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void Given_nullName_When_constructingStudent_Then_throwsArgumentNullException()
    {
        var action = () => new Student(Guid.NewGuid(), null!, "alex@example.com");

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Given_nullEmail_When_constructingStudent_Then_throwsArgumentNullException()
    {
        var action = () => new Student(Guid.NewGuid(), "Alex", null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Given_selfTutorId_When_bookingSession_Then_throwsArgumentException()
    {
        var studentId = Guid.NewGuid();
        var student = new Student(studentId, "Alex", "alex@example.com");

        var action = () => student.BookSession(studentId, CreateSampleInterval());

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Given_emptyTutorId_When_bookingSession_Then_throwsArgumentException()
    {
        var student = new Student(Guid.NewGuid(), "Alex", "alex@example.com");

        var action = () => student.BookSession(Guid.Empty, CreateSampleInterval());

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Given_nullInterval_When_bookingSession_Then_throwsArgumentNullException()
    {
        var student = new Student(Guid.NewGuid(), "Alex", "alex@example.com");

        var action = () => student.BookSession(Guid.NewGuid(), null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Given_nullSession_When_addingSession_Then_throwsArgumentNullException()
    {
        var student = new Student(Guid.NewGuid(), "Alex", "alex@example.com");

        var action = () => student.AddSession(null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Given_mismatchedStudent_When_addingSession_Then_throwsInvalidOperationException()
    {
        var studentId = Guid.NewGuid();
        var student = new Student(studentId, "Alex", "alex@example.com");
        var otherStudentSession = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval());

        var action = () => student.AddSession(otherStudentSession);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Given_duplicateSessionId_When_addingSession_Then_throwsInvalidOperationException()
    {
        var studentId = Guid.NewGuid();
        var student = new Student(studentId, "Alex", "alex@example.com");
        var session = Session.Create(Guid.NewGuid(), studentId, CreateSampleInterval());
        student.AddSession(session);

        var action = () => student.AddSession(session);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Given_existingSession_When_cancellingSession_Then_cancelsIt()
    {
        var studentId = Guid.NewGuid();
        var student = new Student(studentId, "Alex", "alex@example.com");
        var session = Session.Create(Guid.NewGuid(), studentId, CreateSampleInterval());
        student.AddSession(session);

        student.CancelSession(session.Id);

        session.Status.Should().Be(SessionStatus.Cancelled);
    }

    [Fact]
    public void Given_nonBookedSessionWithinInterval_When_bookingSession_Then_allowsNewBooking()
    {
        var studentId = Guid.NewGuid();
        var student = new Student(studentId, "Alex", "alex@example.com");
        var interval = new DateRange(new DateTime(2026, 11, 1, 10, 0, 0), new DateTime(2026, 11, 1, 12, 0, 0));
        var cancelledSession = Session.Create(Guid.NewGuid(), studentId, interval);
        cancelledSession.Cancel();
        student.AddSession(cancelledSession);

        var action = () => student.BookSession(Guid.NewGuid(), interval);

        action.Should().NotThrow();
    }

    [Fact]
    public void Given_privateConstructor_When_invokingViaReflection_Then_createsInstance()
    {
        var ctor = typeof(Student).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, binder: null, Type.EmptyTypes, modifiers: null)!;

        var instance = ctor.Invoke(null);

        instance.Should().NotBeNull();
        ((Student)instance!).Sessions.Should().BeEmpty();
    }
}
