using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Domain.Tutors.Events;
using TutorMatch.Api.Features.SemanticSearch.Services;
using TutorMatch.Api.Infrastructure.Persistence;

namespace TutorMatch.Api.Features.SemanticSearch.Notifications;

/// <summary>
/// Regenerates and persists the embedding vector of a tutor after the profile was committed
/// (create, profile update, subject or availability update all publish <see cref="TutorSavedNotification"/>).
/// </summary>
public class TutorSavedNotificationHandler
{
    private readonly IOllamaClient _ollamaClient;
    private readonly TutorMatchDbContext _dbContext;
    private readonly ILogger<TutorSavedNotificationHandler>? _logger;
    private readonly TutorEmbeddingWriteLock _writeLock;

    public TutorSavedNotificationHandler(
        IOllamaClient ollamaClient,
        TutorMatchDbContext dbContext,
        ILogger<TutorSavedNotificationHandler>? logger = null,
        TutorEmbeddingWriteLock? writeLock = null)
    {
        _ollamaClient = ollamaClient;
        _dbContext = dbContext;
        _logger = logger;
        _writeLock = writeLock ?? TutorEmbeddingWriteLock.Shared;
    }

    /// <summary>
    /// Embeds <c>Name | Subjects | Bio</c> of the latest committed profile and upserts the vector in TutorEmbeddings.
    /// The profile is re-read from the database inside a per-tutor lock, so out-of-order notifications
    /// cannot overwrite a newer embedding with a stale one.
    /// </summary>
    public async Task HandleAsync(TutorSavedNotification notification, CancellationToken cancellationToken = default)
    {
        if (notification is null || notification.TutorId == Guid.Empty)
        {
            _logger?.LogWarning("TutorSavedNotification is invalid; embedding regeneration skipped.");
            return;
        }

        try
        {
            using var _ = await _writeLock.AcquireAsync(notification.TutorId, cancellationToken);

            var profile = await _dbContext.Tutors
                .AsNoTracking()
                .Where(t => t.Id == notification.TutorId)
                .Select(t => new
                {
                    t.Name,
                    t.Bio,
                    Subjects = t.TutorSubjects.Select(ts => ts.Subject.Name).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (profile is null)
            {
                _logger?.LogDebug("Tutor {TutorId} no longer exists; embedding regeneration skipped.", notification.TutorId);
                return;
            }

            var textToEmbed = TutorEmbeddingText.Build(profile.Name, profile.Subjects, profile.Bio);

            var vector = await _ollamaClient.GenerateEmbeddingAsync(textToEmbed, cancellationToken);
            if (vector is null || vector.Length == 0)
            {
                _logger?.LogWarning("Embedding generation for tutor {TutorId} returned no vector.", notification.TutorId);
                return;
            }

            await UpsertEmbeddingAsync(notification.TutorId, VectorMath.Serialize(vector), cancellationToken);
            _logger?.LogInformation("Embedding for tutor {TutorId} saved.", notification.TutorId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to regenerate the embedding for tutor {TutorId}.", notification.TutorId);
        }
    }

    private async Task UpsertEmbeddingAsync(Guid tutorId, byte[] serializedVector, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.TutorEmbeddings
            .FirstOrDefaultAsync(e => e.TutorId == tutorId, cancellationToken);

        if (existing is not null)
        {
            existing.UpdateEmbedding(serializedVector);
        }
        else
        {
            await _dbContext.TutorEmbeddings.AddAsync(new TutorEmbedding(tutorId, serializedVector), cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Subscribes to the in-process TutorSavedNotification event. Each notification is handled on a
    /// background task in its own DI scope. Dispose the returned subscription to unsubscribe.
    /// </summary>
    public static IDisposable RegisterSubscriber(IServiceScopeFactory scopeFactory)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);

        Action<TutorSavedNotification> subscriber = notification =>
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<TutorSavedNotificationHandler>();
                    await handler.HandleAsync(notification);
                }
                catch
                {
                    // Never let a background failure surface as an unobserved exception.
                }
            });
        };

        TutorSavedNotification.Handler += subscriber;
        return new Subscription(subscriber);
    }

    private sealed class Subscription : IDisposable
    {
        private Action<TutorSavedNotification>? _subscriber;

        public Subscription(Action<TutorSavedNotification> subscriber)
        {
            _subscriber = subscriber;
        }

        public void Dispose()
        {
            var subscriber = Interlocked.Exchange(ref _subscriber, null);
            if (subscriber is not null)
            {
                TutorSavedNotification.Handler -= subscriber;
            }
        }
    }
}
