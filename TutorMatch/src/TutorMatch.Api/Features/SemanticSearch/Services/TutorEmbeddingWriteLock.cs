using System.Collections.Concurrent;

namespace TutorMatch.Api.Features.SemanticSearch.Services;

/// <summary>
/// Serializes embedding regeneration per tutor. Background notifications for the same tutor
/// (e.g. two quick profile updates) run one after another, so the read-embed-write sequence
/// never interleaves and the last writer always embeds the latest committed profile.
/// Different tutors are processed in parallel.
/// </summary>
public sealed class TutorEmbeddingWriteLock
{
    // One semaphore per tutor id; the set is bounded by the number of tutors.
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public static TutorEmbeddingWriteLock Shared { get; } = new();

    public async Task<IDisposable> AcquireAsync(Guid tutorId, CancellationToken cancellationToken = default)
    {
        var semaphore = _locks.GetOrAdd(tutorId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);
        return new Releaser(semaphore);
    }

    private sealed class Releaser : IDisposable
    {
        private SemaphoreSlim? _semaphore;

        public Releaser(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _semaphore, null)?.Release();
        }
    }
}
