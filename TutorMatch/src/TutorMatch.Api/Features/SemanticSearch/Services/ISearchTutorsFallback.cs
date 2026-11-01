namespace TutorMatch.Api.Features.SemanticSearch.Services;

/// <summary>
/// Interfata / stub provizoriu pentru mecanismul de fallback catre cautarea clasica (Discovery / SearchTutors).
/// Permite decuplarea modulului de cautare semantica de implementarea finala a Discovery-ului.
/// </summary>
public interface ISearchTutorsFallback
{
    /// <summary>
    /// Executa o cautare clasica bazata pe cuvinte cheie.
    /// </summary>
    /// <param name="query">Termenul de cautare.</param>
    /// <param name="cancellationToken">Token de anulare optional.</param>
    /// <returns>Lista identificatorilor de tutori gasiti.</returns>
    Task<IReadOnlyList<Guid>> SearchAsync(string query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Stub provizoriu pentru decuplare si teste.
/// </summary>
public class DefaultSearchTutorsFallback : ISearchTutorsFallback
{
    public Task<IReadOnlyList<Guid>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
    }
}

