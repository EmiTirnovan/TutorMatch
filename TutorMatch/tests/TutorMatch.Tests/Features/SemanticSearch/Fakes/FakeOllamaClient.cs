using TutorMatch.Api.Features.SemanticSearch.Services;

namespace TutorMatch.Tests.Features.SemanticSearch.Fakes;

public class FakeOllamaClient : IOllamaClient
{
    public Func<string, float[]?>? EmbeddingGenerator { get; set; }
    public List<string> CapturedPrompts { get; } = new();
    public Exception? ExceptionToThrow { get; set; }

    public Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        CapturedPrompts.Add(text);

        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        if (EmbeddingGenerator is not null)
        {
            return Task.FromResult(EmbeddingGenerator(text));
        }

        // Returneaza un vector default de test
        return Task.FromResult<float[]?>([0.1f, 0.2f, 0.3f]);
    }

    public Task<float[]?> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default) =>
        GenerateEmbeddingAsync(text, cancellationToken);
}
