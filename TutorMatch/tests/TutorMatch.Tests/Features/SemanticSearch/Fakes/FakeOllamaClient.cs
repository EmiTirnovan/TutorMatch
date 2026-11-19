using TutorMatch.Api.Features.SemanticSearch.Services;

namespace TutorMatch.Tests.Features.SemanticSearch.Fakes;

public class FakeOllamaClient : IOllamaClient
{
    private int _callCount;
    private int _activeCalls;
    private int _maxConcurrentCalls;

    public Func<string, float[]?>? EmbeddingGenerator { get; set; }
    public List<string> CapturedPrompts { get; } = new();
    public Exception? ExceptionToThrow { get; set; }

    // Simulated model latency; honours the cancellation token like the real HTTP client.
    public TimeSpan Delay { get; set; } = TimeSpan.Zero;

    public int CallCount => Volatile.Read(ref _callCount);
    public int MaxConcurrentCalls => Volatile.Read(ref _maxConcurrentCalls);

    public async Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _callCount);
        lock (CapturedPrompts)
        {
            CapturedPrompts.Add(text);
        }

        var active = Interlocked.Increment(ref _activeCalls);
        UpdateMaxConcurrency(active);

        try
        {
            if (Delay > TimeSpan.Zero)
            {
                await Task.Delay(Delay, cancellationToken);
            }

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            if (EmbeddingGenerator is not null)
            {
                return EmbeddingGenerator(text);
            }

            // Returneaza un vector default de test
            return [0.1f, 0.2f, 0.3f];
        }
        finally
        {
            Interlocked.Decrement(ref _activeCalls);
        }
    }

    public Task<float[]?> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default) =>
        GenerateEmbeddingAsync(text, cancellationToken);

    private void UpdateMaxConcurrency(int active)
    {
        int observed;
        do
        {
            observed = Volatile.Read(ref _maxConcurrentCalls);
        }
        while (active > observed && Interlocked.CompareExchange(ref _maxConcurrentCalls, active, observed) != observed);
    }
}
