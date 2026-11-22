using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Features.Discovery.SearchTutors;
using TutorMatch.Api.Features.SemanticSearch.Services;
using TutorMatch.Api.Infrastructure.Persistence;

namespace TutorMatch.Api.Features.SemanticSearch;

/// <summary>
/// Handler pentru cautarea semantica de tutori cu suport de fallback rezilient catre modulul Discovery (SearchTutors).
/// Query embeddings are cached in memory for 30 minutes and must arrive within a strict time budget (3.5 s by default);
/// a timeout or any Ollama failure falls back to keyword search instead of failing the request.
/// </summary>
public partial class SemanticSearchQueryHandler : IQueryHandler<SemanticSearchQuery, Result<SemanticSearchResponse>>
{
    private readonly IOllamaClient _ollamaClient;
    private readonly TutorMatchDbContext _dbContext;
    private readonly IQueryHandler<SearchTutorsQuery, Result<SearchTutorsResponse>> _fallbackSearchHandler;
    private readonly IMemoryCache? _queryEmbeddingCache;
    private readonly ILogger<SemanticSearchQueryHandler>? _logger;
    private readonly TimeSpan _queryTimeout;
    private readonly string _model;

    public const string ModeSemantic = "semantic";
    public const string ModeKeyword = "keyword";
    public const string FallbackMessage = "Serviciul de cautare semantica IA este momentan indisponibil. S-a realizat fallback catre cautarea clasica dupa cuvinte cheie.";
    public const string QueryEmbeddingCacheKeyPrefix = "semantic-search:query-embedding:";
    public static readonly TimeSpan QueryEmbeddingCacheTtl = TimeSpan.FromMinutes(30);

    public SemanticSearchQueryHandler(
        IOllamaClient ollamaClient,
        TutorMatchDbContext dbContext,
        IQueryHandler<SearchTutorsQuery, Result<SearchTutorsResponse>> fallbackSearchHandler,
        IMemoryCache? queryEmbeddingCache = null,
        IOptions<OllamaOptions>? options = null,
        ILogger<SemanticSearchQueryHandler>? logger = null)
    {
        _ollamaClient = ollamaClient;
        _dbContext = dbContext;
        _fallbackSearchHandler = fallbackSearchHandler;
        _queryEmbeddingCache = queryEmbeddingCache;
        _logger = logger;

        var resolvedOptions = options?.Value ?? new OllamaOptions();
        var timeoutMs = resolvedOptions.QueryTimeoutMilliseconds > 0
            ? resolvedOptions.QueryTimeoutMilliseconds
            : OllamaOptions.DefaultQueryTimeoutMilliseconds;
        _queryTimeout = TimeSpan.FromMilliseconds(timeoutMs);
        _model = string.IsNullOrWhiteSpace(resolvedOptions.Model) ? "default" : resolvedOptions.Model.Trim();
    }

    /// <summary>
    /// Normalizes a search query so equivalent inputs share one cache entry: trim, collapse whitespace, lowercase.
    /// </summary>
    public static string NormalizeQuery(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return string.Empty;
        }

        return WhitespaceRegex().Replace(query.Trim(), " ").ToLowerInvariant();
    }

    public async Task<Result<SemanticSearchResponse>> HandleAsync(
        SemanticSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = NormalizeQuery(query?.Query);
        if (normalizedQuery.Length == 0)
        {
            return Result<SemanticSearchResponse>.Success(new SemanticSearchResponse(
                Array.Empty<SemanticSearchResultItem>(),
                ModeSemantic));
        }

        var queryEmbedding = await GetQueryEmbeddingAsync(normalizedQuery, cancellationToken);

        if (queryEmbedding is null)
        {
            // The keyword search matches subject names case-sensitively, so it gets the original (trimmed) text.
            return await ExecuteFallbackAsync(query!.Query.Trim(), cancellationToken);
        }

        var tutorEmbeddings = await _dbContext.TutorEmbeddings
            .Include(e => e.Tutor)
                .ThenInclude(t => t.TutorSubjects)
                    .ThenInclude(ts => ts.Subject)
            .ToListAsync(cancellationToken);

        if (tutorEmbeddings.Count == 0)
        {
            return Result<SemanticSearchResponse>.Success(new SemanticSearchResponse(
                Array.Empty<SemanticSearchResultItem>(),
                ModeSemantic));
        }

        var scoredTutors = new List<(Tutor Tutor, float Score)>();

        foreach (var te in tutorEmbeddings)
        {
            if (te.Embedding == null || te.Embedding.Length == 0 || te.Tutor == null)
            {
                continue;
            }

            var tutorVector = VectorMath.Deserialize(te.Embedding);
            var similarity = VectorMath.CosineSimilarity(queryEmbedding, tutorVector);

            scoredTutors.Add((te.Tutor, similarity));
        }

        var topResults = scoredTutors
            .OrderByDescending(x => x.Score)
            .Take(5)
            .Select(x => new SemanticSearchResultItem(
                x.Tutor.Id,
                x.Tutor.Name,
                x.Tutor.Bio,
                x.Tutor.HourlyRate,
                x.Score,
                x.Tutor.TutorSubjects
                    .Select(ts => ts.Subject?.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name!)
                    .ToList()))
            .ToList();

        return Result<SemanticSearchResponse>.Success(new SemanticSearchResponse(topResults, ModeSemantic));
    }

    // Returns the cached or freshly generated query vector, or null when Ollama times out, fails or returns nothing.
    private async Task<float[]?> GetQueryEmbeddingAsync(string normalizedQuery, CancellationToken cancellationToken)
    {
        var cacheKey = $"{QueryEmbeddingCacheKeyPrefix}{_model}:{normalizedQuery}";

        if (_queryEmbeddingCache is not null
            && _queryEmbeddingCache.TryGetValue(cacheKey, out float[]? cachedVector)
            && cachedVector is { Length: > 0 })
        {
            return cachedVector;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_queryTimeout);

        float[]? vector;
        try
        {
            // WaitAsync enforces the budget even if the client ignores the cancellation token.
            vector = await _ollamaClient
                .GetEmbeddingAsync(normalizedQuery, timeoutCts.Token)
                .WaitAsync(_queryTimeout, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is TimeoutException or OperationCanceledException)
        {
            timeoutCts.Cancel();
            _logger?.LogWarning(
                "Ollama did not return the query embedding within {TimeoutMs} ms. Falling back to keyword search.",
                _queryTimeout.TotalMilliseconds);
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Ollama query embedding failed. Falling back to keyword search.");
            return null;
        }

        if (vector is null || vector.Length == 0)
        {
            return null;
        }

        _queryEmbeddingCache?.Set(cacheKey, vector, QueryEmbeddingCacheTtl);
        return vector;
    }

    private async Task<Result<SemanticSearchResponse>> ExecuteFallbackAsync(
        string queryString,
        CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Executare fallback catre SearchTutors pentru interogarea: {Query}", queryString);

        try
        {
            var fallbackQuery = new SearchTutorsQuery(queryString);
            var fallbackResult = await _fallbackSearchHandler.HandleAsync(fallbackQuery, cancellationToken);

            if (fallbackResult.IsFailure)
            {
                return Result<SemanticSearchResponse>.Failure(fallbackResult.Error);
            }

            var searchResponse = fallbackResult.Value;
            var results = new List<SemanticSearchResultItem>();

            if (searchResponse?.Tutors != null)
            {
                foreach (var tutorDto in searchResponse.Tutors)
                {
                    var bio = await _dbContext.Tutors
                        .Where(t => t.Id == tutorDto.Id)
                        .Select(t => t.Bio)
                        .FirstOrDefaultAsync(cancellationToken);

                    results.Add(new SemanticSearchResultItem(
                        tutorDto.Id,
                        tutorDto.Name,
                        bio ?? string.Empty,
                        tutorDto.HourlyRate,
                        0f,
                        tutorDto.Subjects));
                }
            }

            return Result<SemanticSearchResponse>.Success(new SemanticSearchResponse(
                results,
                ModeKeyword,
                FallbackMessage));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Eroare la executarea fallback-ului SearchTutors.");
            return Result<SemanticSearchResponse>.Success(new SemanticSearchResponse(
                Array.Empty<SemanticSearchResultItem>(),
                ModeKeyword,
                FallbackMessage));
        }
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
