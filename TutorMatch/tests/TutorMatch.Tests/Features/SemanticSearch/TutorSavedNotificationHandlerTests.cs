using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Domain.Tutors.Events;
using TutorMatch.Api.Features.SemanticSearch.Notifications;
using TutorMatch.Api.Features.SemanticSearch.Services;
using TutorMatch.Api.Infrastructure.Persistence;
using TutorMatch.Tests.Features.SemanticSearch.Fakes;
using Xunit;

namespace TutorMatch.Tests.Features.SemanticSearch;

public class TutorSavedNotificationHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TutorMatchDbContext _context;
    private readonly FakeOllamaClient _fakeOllama;

    public TutorSavedNotificationHandlerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TutorMatchDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new TutorMatchDbContext(options);
        _context.Database.EnsureCreated();

        _fakeOllama = new FakeOllamaClient();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Given_ValidNotification_When_Handled_Then_EmbedsConcatenatedTextAndPersistsVector()
    {
        // Arrange
        var tutorId = Guid.NewGuid();
        var tutor = new Tutor("Tudor Matei", "tudor@test.ro", "Profesor cu experienta in algoritmi.", 100m, tutorId);
        _context.Tutors.Add(tutor);
        await _context.SaveChangesAsync();

        float[] expectedVector = [0.15f, -0.42f, 0.99f];
        _fakeOllama.EmbeddingGenerator = _ => expectedVector;

        var handler = new TutorSavedNotificationHandler(_fakeOllama, _context);
        var notification = new TutorSavedNotification(tutorId, tutor.Bio, new List<string> { "Informatica", "Algoritmi" });

        // Act
        await handler.HandleAsync(notification);

        // Assert
        _fakeOllama.CapturedPrompts.Should().ContainSingle();
        _fakeOllama.CapturedPrompts[0].Should().Be("Profesor cu experienta in algoritmi. Informatica, Algoritmi");

        var persisted = await _context.TutorEmbeddings.FirstOrDefaultAsync(e => e.TutorId == tutorId);
        persisted.Should().NotBeNull();
        persisted!.Embedding.Should().NotBeEmpty();

        var deserialized = VectorMath.Deserialize(persisted.Embedding);
        deserialized.Should().Equal(expectedVector);
    }

    [Fact]
    public async Task Given_ExistingEmbedding_When_HandledAgain_Then_UpdatesExistingEmbedding()
    {
        // Arrange
        var tutorId = Guid.NewGuid();
        var tutor = new Tutor("Tudor Matei", "tudor@test.ro", "Bio initiala.", 100m, tutorId);
        _context.Tutors.Add(tutor);

        byte[] initialVector = VectorMath.Serialize([0.1f, 0.2f]);
        _context.TutorEmbeddings.Add(new TutorEmbedding(tutorId, initialVector));
        await _context.SaveChangesAsync();

        float[] updatedVector = [0.77f, 0.88f, 0.99f];
        _fakeOllama.EmbeddingGenerator = _ => updatedVector;

        var handler = new TutorSavedNotificationHandler(_fakeOllama, _context);
        var notification = new TutorSavedNotification(tutorId, "Bio actualizata.", new List<string> { "Matematica" });

        // Act
        await handler.HandleAsync(notification);

        // Assert
        var persisted = await _context.TutorEmbeddings.FirstOrDefaultAsync(e => e.TutorId == tutorId);
        persisted.Should().NotBeNull();

        var deserialized = VectorMath.Deserialize(persisted!.Embedding);
        deserialized.Should().Equal(updatedVector);
    }

    [Fact]
    public async Task Given_OllamaFailsAndReturnsNull_When_Handled_Then_DoesNotSaveEmbeddingAndDoesNotThrow()
    {
        // Arrange
        var tutorId = Guid.NewGuid();
        var tutor = new Tutor("Elena Pop", "elena@test.ro", "Bio.", 90m, tutorId);
        _context.Tutors.Add(tutor);
        await _context.SaveChangesAsync();

        _fakeOllama.EmbeddingGenerator = _ => null; // Simuleaza caderea serviciului Ollama

        var handler = new TutorSavedNotificationHandler(_fakeOllama, _context);
        var notification = new TutorSavedNotification(tutorId, tutor.Bio, new List<string> { "Chimie" });

        // Act
        var act = async () => await handler.HandleAsync(notification);

        // Assert
        await act.Should().NotThrowAsync();
        var count = await _context.TutorEmbeddings.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task Given_InvalidNotification_When_Handled_Then_ReturnsGracefullyWithoutThrowing()
    {
        // Arrange
        var handler = new TutorSavedNotificationHandler(_fakeOllama, _context);

        // Act & Assert
        await handler.HandleAsync(null!);
        await handler.HandleAsync(new TutorSavedNotification(Guid.Empty, "Bio", new List<string>()));

        _fakeOllama.CapturedPrompts.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_NotificationWithNoSubjects_When_Handled_Then_EmbedsBioOnly()
    {
        // Arrange
        var tutorId = Guid.NewGuid();
        var tutor = new Tutor("Radu", "radu@test.ro", "Ajut la matematica.", 80m, tutorId);
        _context.Tutors.Add(tutor);
        await _context.SaveChangesAsync();

        var handler = new TutorSavedNotificationHandler(_fakeOllama, _context);
        var notification = new TutorSavedNotification(tutorId, tutor.Bio, new List<string>());

        // Act
        await handler.HandleAsync(notification);

        // Assert
        _fakeOllama.CapturedPrompts.Should().ContainSingle();
        _fakeOllama.CapturedPrompts[0].Should().Be("Ajut la matematica.");
    }

    [Fact]
    public async Task Given_NotificationWithNoBioAndHasSubjects_When_Handled_Then_EmbedsSubjectsOnly()
    {
        // Arrange
        var tutorId = Guid.NewGuid();
        var tutor = new Tutor("Ana", "ana@test.ro", "Bio", 80m, tutorId);
        _context.Tutors.Add(tutor);
        await _context.SaveChangesAsync();

        var handler = new TutorSavedNotificationHandler(_fakeOllama, _context);
        var notification = new TutorSavedNotification(tutorId, "", new List<string> { "Fizica", "Mecanica" });

        // Act
        await handler.HandleAsync(notification);

        // Assert
        _fakeOllama.CapturedPrompts.Should().ContainSingle();
        _fakeOllama.CapturedPrompts[0].Should().Be("Fizica, Mecanica");
    }

    [Fact]
    public async Task Given_NotificationWithEmptyBioAndEmptySubjects_When_Handled_Then_ReturnsEarly()
    {
        // Arrange
        var tutorId = Guid.NewGuid();
        var handler = new TutorSavedNotificationHandler(_fakeOllama, _context);
        var notification = new TutorSavedNotification(tutorId, "   ", new List<string>());

        // Act
        await handler.HandleAsync(notification);

        // Assert
        _fakeOllama.CapturedPrompts.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_RegisterSubscriber_When_TutorSavedNotificationPublished_Then_DispatchesSuccessfully()
    {
        // Arrange
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddSingleton<IOllamaClient>(_fakeOllama);
        services.AddSingleton(_context);
        services.AddScoped<TutorSavedNotificationHandler>();
        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>();

        var tutorId = Guid.NewGuid();
        var tutor = new Tutor("Vlad", "vlad@test.ro", "Bio mentor", 120m, tutorId);
        _context.Tutors.Add(tutor);
        await _context.SaveChangesAsync();

        _fakeOllama.EmbeddingGenerator = _ => [0.1f, 0.2f];

        TutorSavedNotificationHandler.RegisterSubscriber(scopeFactory);

        // Act
        TutorSavedNotification.Publish(new TutorSavedNotification(tutorId, tutor.Bio, new List<string> { "React" }));

        // Wait a short moment for background task
        await Task.Delay(200);

        // Assert
        var persisted = await _context.TutorEmbeddings.FirstOrDefaultAsync(e => e.TutorId == tutorId);
        persisted.Should().NotBeNull();
    }
}
