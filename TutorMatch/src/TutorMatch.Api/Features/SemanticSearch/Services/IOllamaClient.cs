namespace TutorMatch.Api.Features.SemanticSearch.Services;

/// <summary>
/// Contract pentru interactiunea cu serviciul Ollama in scopul generarii de vectori de embedding.
/// </summary>
public interface IOllamaClient
{
    /// <summary>
    /// Genereaza vectorul de embedding pentru textul furnizat folosind endpoint-ul /api/embed.
    /// </summary>
    /// <param name="text">Textul pentru care se doreste generarea embedding-ului.</param>
    /// <param name="cancellationToken">Token de anulare.</param>
    /// <returns>Vectorul float[] corespunzator sau null daca operatiunea nu a reusit.</returns>
    Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}

