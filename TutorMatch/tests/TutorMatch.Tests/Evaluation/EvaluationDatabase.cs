using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Features.Discovery.SearchTutors;
using TutorMatch.Api.Features.SemanticSearch;
using TutorMatch.Api.Features.SemanticSearch.Notifications;
using TutorMatch.Api.Features.SemanticSearch.Services;
using TutorMatch.Api.Infrastructure.Persistence;

namespace TutorMatch.Tests.Evaluation;

/// <summary>
/// In-memory SQLite database seeded with <see cref="SemanticSearchEvaluationTests.MockTutorPool"/> and indexed
/// through the production <see cref="TutorSavedNotificationHandler"/> (same "Name | Subjects | Bio" text as the API).
/// Exposes the production semantic and keyword search handlers as evaluation strategies.
/// </summary>
public sealed class EvaluationDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private EvaluationDatabase(SqliteConnection connection, TutorMatchDbContext context)
    {
        _connection = connection;
        Context = context;
    }

    public TutorMatchDbContext Context { get; }

    public static async Task<EvaluationDatabase> CreateAsync(IOllamaClient indexingClient, CancellationToken cancellationToken = default)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);
        var context = new TutorMatchDbContext(new DbContextOptionsBuilder<TutorMatchDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync(cancellationToken);

        var subjects = SemanticSearchEvaluationTests.MockTutorPool
            .SelectMany(t => t.Subjects)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(name => name, name => new Subject(name), StringComparer.OrdinalIgnoreCase);

        var tutors = SemanticSearchEvaluationTests.MockTutorPool
            .Select(profile =>
            {
                var tutor = new Tutor(profile.Name, $"{profile.Id:N}@eval.tutormatch.ro", profile.Bio, 100m, profile.Id);
                profile.Subjects.ForEach(name => tutor.AddSubject(subjects[name]));
                return tutor;
            })
            .ToList();

        context.Tutors.AddRange(tutors);
        await context.SaveChangesAsync(cancellationToken);

        var indexer = new TutorSavedNotificationHandler(indexingClient, context, logger: null, writeLock: new TutorEmbeddingWriteLock());
        foreach (var tutor in tutors)
        {
            await indexer.HandleAsync(tutor.CreateTutorSavedNotification(), cancellationToken);
        }

        context.ChangeTracker.Clear();
        return new EvaluationDatabase(connection, context);
    }

    public Task<int> CountIndexedTutorsAsync(CancellationToken cancellationToken = default) =>
        Context.TutorEmbeddings.CountAsync(cancellationToken);

    /// <summary>The production semantic search handler (no query cache, so every query hits the model).</summary>
    public Func<string, CancellationToken, Task<RankedSearchResult>> SemanticStrategy(IOllamaClient queryClient, OllamaOptions options)
    {
        var handler = new SemanticSearchQueryHandler(
            queryClient,
            Context,
            new SearchTutorsQueryHandler(Context),
            queryEmbeddingCache: null,
            Options.Create(options));

        return async (query, cancellationToken) =>
        {
            var result = await handler.HandleAsync(new SemanticSearchQuery(query), cancellationToken);
            return new RankedSearchResult(
                result.Value.Results.Select(r => r.TutorId).ToList(),
                result.Value.Mode == SemanticSearchQueryHandler.ModeKeyword);
        };
    }

    /// <summary>The classic keyword search exactly as the fallback and the frontend call it: the raw query as subject filter.</summary>
    public Func<string, CancellationToken, Task<RankedSearchResult>> KeywordStrategy()
    {
        var handler = new SearchTutorsQueryHandler(Context);

        return async (query, cancellationToken) =>
        {
            var result = await handler.HandleAsync(new SearchTutorsQuery(query), cancellationToken);
            return new RankedSearchResult(result.Value.Tutors.Select(t => t.Id).ToList(), IsFallback: false);
        };
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
