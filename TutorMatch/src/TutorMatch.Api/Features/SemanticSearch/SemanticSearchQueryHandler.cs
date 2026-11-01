using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Features.Discovery.SearchTutors;
using TutorMatch.Api.Features.SemanticSearch.Services;
using TutorMatch.Api.Infrastructure.Persistence;

namespace TutorMatch.Api.Features.SemanticSearch;

/// <summary>
/// Handler pentru cautarea semantica de tutori cu suport de fallback rezilient catre modulul Discovery (SearchTutors).
/// </summary>
public class SemanticSearchQueryHandler : IQueryHandler<SemanticSearchQuery, Result<SemanticSearchResponse>>
{
    private readonly IOllamaClient _ollamaClient;
    private readonly TutorMatchDbContext _dbContext;
    private readonly IQueryHandler<SearchTutorsQuery, Result<SearchTutorsResponse>> _fallbackSearchHandler;
    private readonly ITutorRepository? _tutorRepository;
    private readonly ILogger<SemanticSearchQueryHandler>? _logger;

    public const string ModeSemantic = "semantic";
    public const string ModeKeyword = "keyword";
    public const string FallbackMessage = "Serviciul de cautare semantica IA este momentan indisponibil. S-a realizat fallback catre cautarea clasica dupa cuvinte cheie.";

    public SemanticSearchQueryHandler(
        IOllamaClient ollamaClient,
        TutorMatchDbContext dbContext,
        IQueryHandler<SearchTutorsQuery, Result<SearchTutorsResponse>> fallbackSearchHandler,
        ITutorRepository? tutorRepository = null,
        ILogger<SemanticSearchQueryHandler>? logger = null)
    {
        _ollamaClient = ollamaClient;
        _dbContext = dbContext;
        _fallbackSearchHandler = fallbackSearchHandler;
        _tutorRepository = tutorRepository;
        _logger = logger;
    }

    public async Task<Result<SemanticSearchResponse>> HandleAsync(
        SemanticSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query is null || string.IsNullOrWhiteSpace(query.Query))
        {
            return Result<SemanticSearchResponse>.Success(new SemanticSearchResponse(
                Array.Empty<SemanticSearchResultItem>(),
                ModeSemantic));
        }

        float[]? queryEmbedding = null;

        try
        {
            // 1. Incercam obtinerea embedding-ului pentru interogare de la Ollama
            queryEmbedding = await _ollamaClient.GetEmbeddingAsync(query.Query, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Apelul catre Ollama a esuat cu exceptie. Se declanseaza fallback catre cautarea clasica.");
        }

        // 2. Daca Ollama nu a intors un vector valid, declansam fallback-ul rezilient
        if (queryEmbedding is null || queryEmbedding.Length == 0)
        {
            return await ExecuteFallbackAsync(query.Query, cancellationToken);
        }

        // 3. Cautare semantica: incarcam toti tutorii si embedding-urile din SQLite
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

        // Ordonam descrescator dupa scorul de similaritate si selectam primii 5
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
                    // Obtinem bio si materii din repo/db pentru un raspuns complet
                    var tutor = await _dbContext.Tutors
                        .Include(t => t.TutorSubjects)
                            .ThenInclude(ts => ts.Subject)
                        .FirstOrDefaultAsync(t => t.Id == tutorDto.Id, cancellationToken);

                    var subjects = tutor?.TutorSubjects
                        .Select(ts => ts.Subject?.Name)
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s!)
                        .ToList() ?? new List<string>();

                    results.Add(new SemanticSearchResultItem(
                        tutorDto.Id,
                        tutorDto.Name,
                        tutor?.Bio ?? string.Empty,
                        tutorDto.HourlyRate,
                        0f,
                        subjects));
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
}

