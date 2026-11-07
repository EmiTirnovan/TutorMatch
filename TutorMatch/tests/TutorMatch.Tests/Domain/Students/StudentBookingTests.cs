using FluentAssertions;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Domain.Students;
using Xunit;

namespace TutorMatch.Tests.Domain.Students;

public class StudentBookingTests
{
    [Fact]
    public void Given_studentWithNoSessions_When_bookingSession_Then_succeedsAndAddsSession()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        var student = new Student(studentId, "Jane Student", "jane@example.com");
        var interval = new DateRange(new DateTime(2026, 11, 2, 10, 0, 0), new DateTime(2026, 11, 2, 11, 0, 0));

        var session = student.BookSession(tutorId, interval);

        session.Should().NotBeNull();
        session.StudentId.Should().Be(studentId);
        session.TutorId.Should().Be(tutorId);
        session.Interval.Should().Be(interval);
        session.Status.Should().Be(SessionStatus.Booked);
        student.Sessions.Should().ContainSingle().Which.Should().Be(session);
    }

    [Fact]
    public void Given_studentWithOneSessionOnSameDay_When_bookingSecondSession_Then_succeedsAndContainsTwoSessions()
    {
        var student = new Student(Guid.NewGuid(), "Jane Student", "jane@example.com");
        var tutor1 = Guid.NewGuid();
        var tutor2 = Guid.NewGuid();
        var session1Interval = new DateRange(new DateTime(2026, 11, 2, 10, 0, 0), new DateTime(2026, 11, 2, 11, 0, 0));
        var session2Interval = new DateRange(new DateTime(2026, 11, 2, 14, 0, 0), new DateTime(2026, 11, 2, 15, 0, 0));
        student.BookSession(tutor1, session1Interval);

        var secondSession = student.BookSession(tutor2, session2Interval);

        secondSession.Should().NotBeNull();
        student.Sessions.Should().HaveCount(2);
    }

    [Fact]
    public void Given_studentWithTwoSessionsOnSameDay_When_bookingThirdSessionOnSameDay_Then_throwsDailySessionLimitExceededException()
    {
        var student = new Student(Guid.NewGuid(), "Jane Student", "jane@example.com");
        var tutor1 = Guid.NewGuid();
        var tutor2 = Guid.NewGuid();
        var tutor3 = Guid.NewGuid();
        var session1Interval = new DateRange(new DateTime(2026, 11, 2, 9, 0, 0), new DateTime(2026, 11, 2, 10, 0, 0));
        var session2Interval = new DateRange(new DateTime(2026, 11, 2, 11, 0, 0), new DateTime(2026, 11, 2, 12, 0, 0));
        var session3Interval = new DateRange(new DateTime(2026, 11, 2, 14, 0, 0), new DateTime(2026, 11, 2, 15, 0, 0));
        student.BookSession(tutor1, session1Interval);
        student.BookSession(tutor2, session2Interval);

        var action = () => student.BookSession(tutor3, session3Interval);

        action.Should().Throw<DailySessionLimitExceededException>()
            .WithMessage("*Student already has 2 booked sessions today*");
    }

    [Fact]
    public void Given_studentWithTwoSessionsOnDifferentDays_When_bookingSessionOnFirstDay_Then_succeeds()
    {
        var student = new Student(Guid.NewGuid(), "Jane Student", "jane@example.com");
        var tutor = Guid.NewGuid();
        var day1Interval1 = new DateRange(new DateTime(2026, 11, 2, 10, 0, 0), new DateTime(2026, 11, 2, 11, 0, 0));
        var day2Interval = new DateRange(new DateTime(2026, 11, 3, 10, 0, 0), new DateTime(2026, 11, 3, 11, 0, 0));
        var day1Interval2 = new DateRange(new DateTime(2026, 11, 2, 14, 0, 0), new DateTime(2026, 11, 2, 15, 0, 0));
        student.BookSession(tutor, day1Interval1);
        student.BookSession(tutor, day2Interval);

        var action = () => student.BookSession(tutor, day1Interval2);

        action.Should().NotThrow();
        student.Sessions.Should().HaveCount(3);
    }

    [Fact]
    public void Given_studentWithCancelledSessionOnSameDay_When_bookingSecondActiveSession_Then_allowsBooking()
    {
        var student = new Student(Guid.NewGuid(), "Jane Student", "jane@example.com");
        var tutor = Guid.NewGuid();
        var interval1 = new DateRange(new DateTime(2026, 11, 2, 9, 0, 0), new DateTime(2026, 11, 2, 10, 0, 0));
        var interval2 = new DateRange(new DateTime(2026, 11, 2, 11, 0, 0), new DateTime(2026, 11, 2, 12, 0, 0));
        var interval3 = new DateRange(new DateTime(2026, 11, 2, 14, 0, 0), new DateTime(2026, 11, 2, 15, 0, 0));
        var session1 = student.BookSession(tutor, interval1);
        student.BookSession(tutor, interval2);
        student.CancelSession(session1.Id);

        var action = () => student.BookSession(tutor, interval3);

        action.Should().NotThrow();
    }

    [Fact]
    public void Given_emptyName_When_constructingStudent_Then_throwsArgumentException()
    {
        var action = () => new Student(Guid.NewGuid(), "", "jane@example.com");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Given_whitespaceName_When_constructingStudent_Then_throwsArgumentException()
    {
        var action = () => new Student(Guid.NewGuid(), "   ", "jane@example.com");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Given_emptyEmail_When_constructingStudent_Then_throwsArgumentException()
    {
        var action = () => new Student(Guid.NewGuid(), "Jane", "");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Given_whitespaceEmail_When_constructingStudent_Then_throwsArgumentException()
    {
        var action = () => new Student(Guid.NewGuid(), "Jane", "   ");

        action.Should().Throw<ArgumentException>();
    }
}
