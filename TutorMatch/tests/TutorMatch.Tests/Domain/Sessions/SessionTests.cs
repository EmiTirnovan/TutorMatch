using FluentAssertions;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Sessions;
using Xunit;

namespace TutorMatch.Tests.Domain.Sessions;

public class SessionTests
{
    private static DateRange CreateSampleInterval(int startOffsetHours = 1, int durationHours = 1)
    {
        var start = DateTime.UtcNow.AddHours(startOffsetHours);
        return new DateRange(start, start.AddHours(durationHours));
    }

    [Fact]
    public void Given_validArguments_When_constructingSession_Then_initializesProperties()
    {
        var id = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var interval = CreateSampleInterval();

        var session = new Session(id, tutorId, studentId, interval);

        session.Id.Should().Be(id);
        session.TutorId.Should().Be(tutorId);
        session.StudentId.Should().Be(studentId);
        session.Interval.Should().Be(interval);
        session.DateRange.Should().Be(interval);
        session.StartTime.Should().Be(interval.Start);
        session.EndTime.Should().Be(interval.End);
        session.Status.Should().Be(SessionStatus.Booked);
        session.Version.Should().Be(1);
    }

    [Fact]
    public void Given_nullInterval_When_constructingSession_Then_throwsArgumentNullException()
    {
        var action = () => new Session(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), (DateRange)null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Given_invalidVersion_When_constructingSession_Then_throwsArgumentException()
    {
        var interval = CreateSampleInterval();
        var action = () => new Session(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), interval, version: 0);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Given_validSession_When_cancellingBookedSession_Then_updatesStatusAndVersion()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval());

        session.Cancel();

        session.Status.Should().Be(SessionStatus.Cancelled);
        session.Version.Should().Be(2);
    }

    [Fact]
    public void Given_validBookedSession_When_lateCancelling_Then_updatesStatusAndVersion()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval());

        session.LateCancel();

        session.Status.Should().Be(SessionStatus.LateCancelled);
        session.Version.Should().Be(2);
    }

    [Fact]
    public void Given_nonBookedSession_When_lateCancelling_Then_throwsInvalidOperationException()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval());
        session.Cancel();

        var action = () => session.LateCancel();

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Given_invalidStatus_When_constructingSession_Then_throwsArgumentOutOfRangeException()
    {
        var interval = CreateSampleInterval();
        var action = () => new Session(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), interval, status: (SessionStatus)999);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Given_validSession_When_rescheduling_Then_updatesIntervalAndVersion()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval(1, 1));
        var newInterval = CreateSampleInterval(3, 1);

        session.Reschedule(newInterval);

        session.Interval.Should().Be(newInterval);
        session.Version.Should().Be(2);
    }

    [Fact]
    public void Given_validSession_When_reschedulingWithDateTimes_Then_updatesIntervalAndVersion()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval(1, 1));
        var newStart = DateTime.UtcNow.AddDays(2);
        var newEnd = newStart.AddHours(2);

        session.Reschedule(newStart, newEnd);

        session.StartTime.Should().Be(newStart);
        session.EndTime.Should().Be(newEnd);
        session.Version.Should().Be(2);
    }

    [Fact]
    public void Given_nullInterval_When_rescheduling_Then_throwsArgumentNullException()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval(1, 1));

        var action = () => session.Reschedule((DateRange)null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Given_completedSession_When_rescheduling_Then_throwsInvalidOperationException()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval(1, 1));
        session.Complete();
        var newInterval = CreateSampleInterval(3, 1);

        var action = () => session.Reschedule(newInterval);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Given_validBookedSession_When_completing_Then_updatesStatusAndVersion()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval());

        session.Complete();

        session.Status.Should().Be(SessionStatus.Completed);
        session.Version.Should().Be(2);
    }

    [Fact]
    public void Given_canceledSession_When_cancelling_Then_throwsInvalidOperationException()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval());
        session.Cancel();

        var action = () => session.Cancel();

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Given_cancelledSession_When_completing_Then_throwsInvalidOperationException()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval());
        session.Cancel();

        var action = () => session.Complete();

        action.Should().Throw<InvalidOperationException>();
    }
}
