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
    public void Constructor_ValidArguments_ShouldInstantiateSessionWithInitialValues()
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
    public void Create_FactoryMethod_ShouldGenerateNewSession()
    {
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var interval = CreateSampleInterval();

        var session = Session.Create(tutorId, studentId, interval);

        session.Id.Should().NotBeEmpty();
        session.TutorId.Should().Be(tutorId);
        session.StudentId.Should().Be(studentId);
        session.Interval.Should().Be(interval);
        session.Status.Should().Be(SessionStatus.Booked);
        session.Version.Should().Be(1);
    }

    [Fact]
    public void Constructor_NullInterval_ShouldThrowArgumentNullException()
    {
        var act = () => new Session(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), (DateRange)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_InvalidVersion_ShouldThrowArgumentException()
    {
        var interval = CreateSampleInterval();
        var act = () => new Session(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), interval, version: 0);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Cancel_WhenBooked_ShouldChangeStatusToCancelledAndIncrementVersion()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval());

        session.Cancel();

        session.Status.Should().Be(SessionStatus.Cancelled);
        session.Version.Should().Be(2);
    }

    [Fact]
    public void Cancel_WhenNotBooked_ShouldThrowInvalidOperationException()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval());
        session.Cancel();

        var act = () => session.Cancel();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void LateCancel_WhenBooked_ShouldChangeStatusToLateCancelledAndIncrementVersion()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval());

        session.LateCancel();

        session.Status.Should().Be(SessionStatus.LateCancelled);
        session.Version.Should().Be(2);
    }

    [Fact]
    public void Complete_WhenBooked_ShouldChangeStatusToCompletedAndIncrementVersion()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval());

        session.Complete();

        session.Status.Should().Be(SessionStatus.Completed);
        session.Version.Should().Be(2);
    }

    [Fact]
    public void Reschedule_WhenBooked_ShouldUpdateIntervalAndIncrementVersion()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval(1, 1));
        var newInterval = CreateSampleInterval(3, 1);

        session.Reschedule(newInterval);

        session.Interval.Should().Be(newInterval);
        session.Version.Should().Be(2);
    }

    [Fact]
    public void Reschedule_WhenNotBooked_ShouldThrowInvalidOperationException()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval(1, 1));
        session.Complete();
        var newInterval = CreateSampleInterval(3, 1);

        var act = () => session.Reschedule(newInterval);

        act.Should().Throw<InvalidOperationException>();
    }
}
