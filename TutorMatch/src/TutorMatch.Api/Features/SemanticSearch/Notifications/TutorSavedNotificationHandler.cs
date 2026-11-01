using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Domain.Tutors.Events;
using TutorMatch.Api.Features.SemanticSearch.Services;
using TutorMatch.Api.Infrastructure.Persistence;

namespace TutorMatch.Api.Features.SemanticSearch.Notifications;

/// <summary>
/// Handler pentru evenimentul TutorSavedNotification, responsabil de generarea si persistarea embedding-urilor vectoriale.
/// </summary>
public class TutorSavedNotificationHandler
{
    private readonly IOllamaClient _ollamaClient;
    private readonly TutorMatchDbContext _dbContext;
    private readonly ILogger<TutorSavedNotificationHandler>? _logger;

    public TutorSavedNotificationHandler(
        IOllamaClient ollamaClient,
        TutorMatchDbContext dbContext,
        ILogger<TutorSavedNotificationHandler>? logger = null)
    {
        _ollamaClient = ollamaClient;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Proceseaza notificarea de salvare a unui tutore: genereaza embedding prin Ollama si salveaza vectorul serializat in SQLite.
    /// </summary>
    public async Task HandleAsync(TutorSavedNotification notification, CancellationToken cancellationToken = default)
    {
        if (notification is null || notification.TutorId == Guid.Empty)
        {
            _logger?.LogWarning("Notificarea TutorSavedNotification este invalida.");
            return;
        }

        try
        {
            // Concateneaza: bio + string.Join(", ", subjects)
            var subjects = notification.Subjects ?? new List<string>();
            var subjectsText = string.Join(", ", subjects);

            string textToEmbed;
            if (string.IsNullOrWhiteSpace(notification.Bio))
            {
                textToEmbed = subjectsText;
            }
            else if (string.IsNullOrWhiteSpace(subjectsText))
            {
                textToEmbed = notification.Bio;
            }
            else
            {
                textToEmbed = $"{notification.Bio} {subjectsText}";
            }

            if (string.IsNullOrWhiteSpace(textToEmbed))
            {
                _logger?.LogDebug("Textul de embedding pentru tutorul {TutorId} este vid.", notification.TutorId);
                return;
            }

            var vector = await _ollamaClient.GenerateEmbeddingAsync(textToEmbed, cancellationToken);
            if (vector is null || vector.Length == 0)
            {
                _logger?.LogWarning(
                    "Generarea embedding-ului pentru tutorul {TutorId} nu a returnat niciun vector.",
                    notification.TutorId);
                return;
            }

            var serializedBytes = VectorMath.Serialize(vector);

            var existing = await _dbContext.TutorEmbeddings
                .FirstOrDefaultAsync(e => e.TutorId == notification.TutorId, cancellationToken);

            if (existing is not null)
            {
                existing.UpdateEmbedding(serializedBytes);
            }
            else
            {
                var embedding = new TutorEmbedding(notification.TutorId, serializedBytes);
                await _dbContext.TutorEmbeddings.AddAsync(embedding, cancellationToken);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger?.LogInformation("Vectorul de embedding pentru tutorul {TutorId} a fost salvat cu succes.", notification.TutorId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Eroare la persistarea embedding-ului pentru tutorul {TutorId}.", notification.TutorId);
        }
    }

    /// <summary>
    /// Inregistreaza abonarea statica la evenimentul in-process TutorSavedNotification.Handler.
    /// </summary>
    public static void RegisterSubscriber(IServiceScopeFactory scopeFactory)
    {
        TutorSavedNotification.Handler += notification =>
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
                    // Prevenim propagarea exceptiilor pe un thread de fundal
                }
            });
        };
    }
}

