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

    [Fact]
    public void Given_validSession_When_incrementingVersion_Then_versionIncreasesByOne()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval());

        session.IncrementVersion();

        session.Version.Should().Be(2);
    }

    [Fact]
    public void Given_matchingVersion_When_verifyingVersion_Then_doesNotThrow()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval());

        var action = () => session.VerifyVersion(1);

        action.Should().NotThrow();
    }

    [Fact]
    public void Given_mismatchedVersion_When_verifyingVersion_Then_throwsInvalidOperationException()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval());

        var action = () => session.VerifyVersion(2);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Concurrency conflict*");
    }

    [Fact]
    public void Given_timeIs12HoursAnd1MinuteBeforeStart_When_cancelling_Then_sessionBecomesCancelled()
    {
        var start = new DateTime(2026, 11, 2, 20, 0, 0, DateTimeKind.Utc);
        var interval = new DateRange(start, start.AddHours(2));
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), interval);
        var fakeClock = new Microsoft.Extensions.Time.Testing.FakeTimeProvider(start.AddHours(-12).AddMinutes(-1));

        session.Cancel(fakeClock);

        session.Status.Should().Be(SessionStatus.Cancelled);
        session.Version.Should().Be(2);
    }

    [Fact]
    public void Given_timeIsExactly12HoursBeforeStart_When_cancelling_Then_sessionBecomesCancelled()
    {
        var start = new DateTime(2026, 11, 2, 20, 0, 0, DateTimeKind.Utc);
        var interval = new DateRange(start, start.AddHours(2));
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), interval);
        var fakeClock = new Microsoft.Extensions.Time.Testing.FakeTimeProvider(start.AddHours(-12));

        session.Cancel(fakeClock);

        session.Status.Should().Be(SessionStatus.Cancelled);
        session.Version.Should().Be(2);
    }

    [Fact]
    public void Given_timeIs11HoursAnd59MinutesBeforeStart_When_cancelling_Then_sessionBecomesLateCancelled()
    {
        var start = new DateTime(2026, 11, 2, 20, 0, 0, DateTimeKind.Utc);
        var interval = new DateRange(start, start.AddHours(2));
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), interval);
        var fakeClock = new Microsoft.Extensions.Time.Testing.FakeTimeProvider(start.AddHours(-11).AddMinutes(-59));

        session.Cancel(fakeClock);

        session.Status.Should().Be(SessionStatus.LateCancelled);
        session.Version.Should().Be(2);
    }

    [Fact]
    public void Given_nullTimeProvider_When_cancelling_Then_throwsArgumentNullException()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval());

        var action = () => session.Cancel((TimeProvider)null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Given_lateCancelledSession_When_cancellingWithTimeProvider_Then_throwsInvalidOperationException()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval());
        session.LateCancel();
        var fakeClock = new Microsoft.Extensions.Time.Testing.FakeTimeProvider(DateTimeOffset.UtcNow);

        var action = () => session.Cancel(fakeClock);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Given_completedSession_When_cancellingWithTimeProvider_Then_throwsInvalidOperationException()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval());
        session.Complete();
        var fakeClock = new Microsoft.Extensions.Time.Testing.FakeTimeProvider(DateTimeOffset.UtcNow);

        var action = () => session.Cancel(fakeClock);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Given_timeBeforeStart_When_completingWithTimeProvider_Then_throwsInvalidOperationException()
    {
        var start = new DateTime(2026, 11, 2, 20, 0, 0, DateTimeKind.Utc);
        var interval = new DateRange(start, start.AddHours(2));
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), interval);
        var fakeClock = new Microsoft.Extensions.Time.Testing.FakeTimeProvider(start.AddMinutes(-5));

        var action = () => session.Complete(fakeClock);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot complete a session before its start time.*");
    }

    [Fact]
    public void Given_timeAtStart_When_completingWithTimeProvider_Then_sessionBecomesCompleted()
    {
        var start = new DateTime(2026, 11, 2, 20, 0, 0, DateTimeKind.Utc);
        var interval = new DateRange(start, start.AddHours(2));
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), interval);
        var fakeClock = new Microsoft.Extensions.Time.Testing.FakeTimeProvider(start);

        session.Complete(fakeClock);

        session.Status.Should().Be(SessionStatus.Completed);
        session.Version.Should().Be(2);
    }

    [Fact]
    public void Given_timeDuringSession_When_completingWithTimeProvider_Then_sessionBecomesCompleted()
    {
        var start = new DateTime(2026, 11, 2, 20, 0, 0, DateTimeKind.Utc);
        var interval = new DateRange(start, start.AddHours(2));
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), interval);
        var fakeClock = new Microsoft.Extensions.Time.Testing.FakeTimeProvider(start.AddHours(1));

        session.Complete(fakeClock);

        session.Status.Should().Be(SessionStatus.Completed);
        session.Version.Should().Be(2);
    }

    [Fact]
    public void Given_timeAfterEnd_When_completingWithTimeProvider_Then_sessionBecomesCompleted()
    {
        var start = new DateTime(2026, 11, 2, 20, 0, 0, DateTimeKind.Utc);
        var interval = new DateRange(start, start.AddHours(2));
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), interval);
        var fakeClock = new Microsoft.Extensions.Time.Testing.FakeTimeProvider(start.AddHours(3));

        session.Complete(fakeClock);

        session.Status.Should().Be(SessionStatus.Completed);
        session.Version.Should().Be(2);
    }

    [Fact]
    public void Given_nullTimeProvider_When_completing_Then_throwsArgumentNullException()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval());

        var action = () => session.Complete((TimeProvider)null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Given_cancelledSession_When_completingWithTimeProvider_Then_throwsInvalidOperationException()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval());
        session.Cancel();
        var fakeClock = new Microsoft.Extensions.Time.Testing.FakeTimeProvider(DateTimeOffset.UtcNow.AddHours(2));

        var action = () => session.Complete(fakeClock);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Given_lateCancelledSession_When_completingWithTimeProvider_Then_throwsInvalidOperationException()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval());
        session.LateCancel();
        var fakeClock = new Microsoft.Extensions.Time.Testing.FakeTimeProvider(DateTimeOffset.UtcNow.AddHours(2));

        var action = () => session.Complete(fakeClock);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Given_completedSession_When_completingAgainWithTimeProvider_Then_throwsInvalidOperationException()
    {
        var session = Session.Create(Guid.NewGuid(), Guid.NewGuid(), CreateSampleInterval());
        session.Complete();
        var fakeClock = new Microsoft.Extensions.Time.Testing.FakeTimeProvider(DateTimeOffset.UtcNow.AddHours(2));

        var action = () => session.Complete(fakeClock);

        action.Should().Throw<InvalidOperationException>();
    }
}
