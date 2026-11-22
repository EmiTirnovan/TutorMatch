using FluentAssertions;
using TutorMatch.Api.Features.SemanticSearch.Services;

namespace TutorMatch.Tests.Features.SemanticSearch;

public class TutorEmbeddingWriteLockTests
{
    [Fact]
    public async Task Given_LockHeldForTutor_When_AcquiringSameTutor_Then_WaitsUntilReleased()
    {
        var writeLock = new TutorEmbeddingWriteLock();
        var tutorId = Guid.NewGuid();
        var firstLease = await writeLock.AcquireAsync(tutorId);

        var secondAcquire = writeLock.AcquireAsync(tutorId);
        var completedBeforeRelease = await Task.WhenAny(secondAcquire, Task.Delay(100)) == secondAcquire;
        firstLease.Dispose();
        using var secondLease = await secondAcquire.WaitAsync(TimeSpan.FromSeconds(5));

        completedBeforeRelease.Should().BeFalse();
        secondLease.Should().NotBeNull();
    }

    [Fact]
    public async Task Given_LockHeldForTutor_When_AcquiringDifferentTutor_Then_DoesNotWait()
    {
        var writeLock = new TutorEmbeddingWriteLock();
        using var firstLease = await writeLock.AcquireAsync(Guid.NewGuid());

        var secondAcquire = writeLock.AcquireAsync(Guid.NewGuid());
        var completedImmediately = await Task.WhenAny(secondAcquire, Task.Delay(TimeSpan.FromSeconds(5))) == secondAcquire;
        using var secondLease = await secondAcquire;

        completedImmediately.Should().BeTrue();
    }

    [Fact]
    public async Task Given_LeaseDisposedTwice_When_AcquiringAgain_Then_OnlyOnePermitIsReleased()
    {
        var writeLock = new TutorEmbeddingWriteLock();
        var tutorId = Guid.NewGuid();
        var lease = await writeLock.AcquireAsync(tutorId);
        lease.Dispose();
        lease.Dispose();
        using var reacquired = await writeLock.AcquireAsync(tutorId);

        var thirdAcquire = writeLock.AcquireAsync(tutorId);
        var completedWhileHeld = await Task.WhenAny(thirdAcquire, Task.Delay(100)) == thirdAcquire;

        completedWhileHeld.Should().BeFalse();
    }

    [Fact]
    public async Task Given_CancelledToken_When_WaitingForHeldLock_Then_ThrowsOperationCanceled()
    {
        var writeLock = new TutorEmbeddingWriteLock();
        var tutorId = Guid.NewGuid();
        using var held = await writeLock.AcquireAsync(tutorId);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        var act = () => writeLock.AcquireAsync(tutorId, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void Given_SharedInstance_When_ReadTwice_Then_ReturnsSameInstance()
    {
        var first = TutorEmbeddingWriteLock.Shared;

        var second = TutorEmbeddingWriteLock.Shared;

        second.Should().BeSameAs(first);
    }
}
