using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Domain.Tutors.Events;
using TutorMatch.Api.Features.SemanticSearch.Notifications;
using TutorMatch.Api.Features.SemanticSearch.Services;
using TutorMatch.Api.Infrastructure.Persistence;
using TutorMatch.Tests.Common;
using TutorMatch.Tests.Features.SemanticSearch.Fakes;

namespace TutorMatch.Tests.Features.SemanticSearch;

public class TutorSavedNotificationHandlerTests : IDisposable
{
    // Named shared-cache in-memory database: several DbContexts (one per connection) can work on it concurrently.
    private readonly string _connectionString = $"Data Source=notif-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private readonly SqliteConnection _keepAliveConnection;
    private readonly TutorMatchDbContext _context;
    private readonly FakeOllamaClient _fakeOllama = new();
    private readonly TutorEmbeddingWriteLock _writeLock = new();

    public TutorSavedNotificationHandlerTests()
    {
        _keepAliveConnection = new SqliteConnection(_connectionString);
        _keepAliveConnection.Open();

        _context = CreateContext();
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _keepAliveConnection.Dispose();
    }

    [Fact]
    public async Task Given_TutorWithSubjects_When_Handled_Then_EmbedsNameSortedSubjectsAndBio()
    {
        var tutor = await SeedTutorAsync("Tudor Matei", "Profesor cu experienta in algoritmi.", "Informatica", "Algoritmi");
        var handler = CreateHandler(_context);

        await handler.HandleAsync(tutor.CreateTutorSavedNotification());

        _fakeOllama.CapturedPrompts.Should().ContainSingle()
            .Which.Should().Be("Tudor Matei | Algoritmi, Informatica | Profesor cu experienta in algoritmi.");
    }

    [Fact]
    public async Task Given_ValidNotification_When_Handled_Then_PersistsSerializedVector()
    {
        var tutor = await SeedTutorAsync("Tudor Matei", "Profesor.", "Informatica");
        float[] expectedVector = [0.15f, -0.42f, 0.99f];
        _fakeOllama.EmbeddingGenerator = _ => expectedVector;
        var handler = CreateHandler(_context);

        await handler.HandleAsync(tutor.CreateTutorSavedNotification());

        var persisted = await ReadVectorAsync(tutor.Id);
        persisted.Should().Equal(expectedVector);
    }

    [Fact]
    public async Task Given_TutorWithoutSubjects_When_Handled_Then_KeepsEmptySubjectSegment()
    {
        var tutor = await SeedTutorAsync("Radu", "Ajut la matematica.");
        var handler = CreateHandler(_context);

        await handler.HandleAsync(tutor.CreateTutorSavedNotification());

        _fakeOllama.CapturedPrompts.Should().ContainSingle().Which.Should().Be("Radu |  | Ajut la matematica.");
    }

    [Fact]
    public async Task Given_ExistingEmbedding_When_HandledAgain_Then_UpdatesTheSingleExistingRow()
    {
        var tutor = await SeedTutorAsync("Tudor Matei", "Bio initiala.");
        _context.TutorEmbeddings.Add(new TutorEmbedding(tutor.Id, VectorMath.Serialize([0.1f, 0.2f])));
        await _context.SaveChangesAsync();
        float[] updatedVector = [0.77f, 0.88f, 0.99f];
        _fakeOllama.EmbeddingGenerator = _ => updatedVector;
        var handler = CreateHandler(_context);

        await handler.HandleAsync(tutor.CreateTutorSavedNotification());

        (await _context.TutorEmbeddings.CountAsync(e => e.TutorId == tutor.Id)).Should().Be(1);
        (await ReadVectorAsync(tutor.Id)).Should().Equal(updatedVector);
    }

    [Fact]
    public async Task Given_BioAndSubjectsUpdatedAfterFirstEmbedding_When_HandledAgain_Then_EmbeddingReflectsNewProfile()
    {
        var tutor = await SeedTutorAsync("Elena Pop", "Predau mecanica clasica.", "Fizica");
        _fakeOllama.EmbeddingGenerator = DeterministicTextEmbedder.Embed;
        var handler = CreateHandler(_context);
        await handler.HandleAsync(tutor.CreateTutorSavedNotification());
        tutor.UpdateProfile(tutor.Name, "Pregatesc studenti pentru astrofizica.", tutor.HourlyRate);
        tutor.UpdateSubjects(new[] { new Subject("Astronomie") });
        await _context.SaveChangesAsync();
        var expectedText = "Elena Pop | Astronomie | Pregatesc studenti pentru astrofizica.";

        await handler.HandleAsync(tutor.CreateTutorSavedNotification());

        _fakeOllama.CapturedPrompts.Last().Should().Be(expectedText);
        (await ReadVectorAsync(tutor.Id)).Should().Equal(DeterministicTextEmbedder.Embed(expectedText));
    }

    [Fact]
    public async Task Given_NotificationWithStaleContent_When_Handled_Then_EmbedsLatestCommittedProfile()
    {
        var tutor = await SeedTutorAsync("Ana", "Bio noua din baza de date.", "Chimie");
        var staleNotification = new TutorSavedNotification(tutor.Id, "Bio veche", new List<string> { "Fizica" });
        var handler = CreateHandler(_context);

        await handler.HandleAsync(staleNotification);

        _fakeOllama.CapturedPrompts.Should().ContainSingle().Which.Should().Be("Ana | Chimie | Bio noua din baza de date.");
    }

    [Fact]
    public async Task Given_OllamaReturnsNull_When_Handled_Then_DoesNotSaveEmbedding()
    {
        var tutor = await SeedTutorAsync("Elena Pop", "Bio.", "Chimie");
        _fakeOllama.EmbeddingGenerator = _ => null;
        var handler = CreateHandler(_context);

        await handler.HandleAsync(tutor.CreateTutorSavedNotification());

        (await _context.TutorEmbeddings.AnyAsync(e => e.TutorId == tutor.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task Given_OllamaThrows_When_Handled_Then_SwallowsErrorAndDoesNotSaveEmbedding()
    {
        var tutor = await SeedTutorAsync("Elena Pop", "Bio.", "Chimie");
        _fakeOllama.ExceptionToThrow = new HttpRequestException("Connection refused.");
        var handler = CreateHandler(_context);

        var act = () => handler.HandleAsync(tutor.CreateTutorSavedNotification());

        await act.Should().NotThrowAsync();
        (await _context.TutorEmbeddings.AnyAsync(e => e.TutorId == tutor.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task Given_NullNotification_When_Handled_Then_DoesNotCallOllama()
    {
        var handler = CreateHandler(_context);

        await handler.HandleAsync(null!);

        _fakeOllama.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_EmptyTutorId_When_Handled_Then_DoesNotCallOllama()
    {
        var handler = CreateHandler(_context);

        await handler.HandleAsync(new TutorSavedNotification(Guid.Empty, "Bio", new List<string>()));

        _fakeOllama.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_TutorMissingFromDatabase_When_Handled_Then_DoesNotCallOllama()
    {
        var handler = CreateHandler(_context);

        await handler.HandleAsync(new TutorSavedNotification(Guid.NewGuid(), "Bio", new List<string> { "Fizica" }));

        _fakeOllama.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_ConcurrentNotificationsForSameTutor_When_Handled_Then_RegenerationsRunSequentiallyAndKeepOneRow()
    {
        var tutor = await SeedTutorAsync("Vlad", "Mentor React.", "React");
        _fakeOllama.Delay = TimeSpan.FromMilliseconds(150);
        await using var firstContext = CreateContext();
        await using var secondContext = CreateContext();
        var notification = tutor.CreateTutorSavedNotification();

        await Task.WhenAll(
            CreateHandler(firstContext).HandleAsync(notification),
            CreateHandler(secondContext).HandleAsync(notification));

        _fakeOllama.CallCount.Should().Be(2);
        _fakeOllama.MaxConcurrentCalls.Should().Be(1);
        (await _context.TutorEmbeddings.CountAsync(e => e.TutorId == tutor.Id)).Should().Be(1);
    }

    [Fact]
    public async Task Given_RegisteredSubscriber_When_NotificationPublished_Then_EmbeddingIsPersistedInBackground()
    {
        var tutor = await SeedTutorAsync("Vlad", "Bio mentor", "React");
        using var provider = BuildSubscriberServiceProvider();
        using var subscription = TutorSavedNotificationHandler.RegisterSubscriber(provider.GetRequiredService<IServiceScopeFactory>());

        TutorSavedNotification.Publish(tutor.CreateTutorSavedNotification());

        var persisted = await Eventually.SatisfiedAsync(
            () => HasEmbeddingAsync(tutor.Id),
            TimeSpan.FromSeconds(5));
        persisted.Should().BeTrue();
    }

    [Fact]
    public async Task Given_DisposedSubscription_When_NotificationPublished_Then_HandlerIsNotInvoked()
    {
        var tutor = await SeedTutorAsync("Vlad", "Bio mentor", "React");
        using var provider = BuildSubscriberServiceProvider();
        var subscription = TutorSavedNotificationHandler.RegisterSubscriber(provider.GetRequiredService<IServiceScopeFactory>());
        subscription.Dispose();
        subscription.Dispose();

        TutorSavedNotification.Publish(tutor.CreateTutorSavedNotification());
        await Task.Delay(200);

        _fakeOllama.CallCount.Should().Be(0);
        (await HasEmbeddingAsync(tutor.Id)).Should().BeFalse();
    }

    [Fact]
    public void Given_NullScopeFactory_When_RegisteringSubscriber_Then_ThrowsArgumentNullException()
    {
        var act = () => TutorSavedNotificationHandler.RegisterSubscriber(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private TutorMatchDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TutorMatchDbContext>()
            .UseSqlite(_connectionString)
            .Options;
        return new TutorMatchDbContext(options);
    }

    private TutorSavedNotificationHandler CreateHandler(TutorMatchDbContext context) =>
        new(_fakeOllama, context, logger: null, writeLock: _writeLock);

    private ServiceProvider BuildSubscriberServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOllamaClient>(_fakeOllama);
        services.AddSingleton(_writeLock);
        services.AddDbContext<TutorMatchDbContext>(options => options.UseSqlite(_connectionString));
        services.AddScoped<TutorSavedNotificationHandler>();
        return services.BuildServiceProvider();
    }

    private async Task<Tutor> SeedTutorAsync(string name, string bio, params string[] subjectNames)
    {
        var tutor = new Tutor(name, $"{Guid.NewGuid():N}@test.ro", bio, 100m);
        foreach (var subjectName in subjectNames)
        {
            tutor.AddSubject(new Subject(subjectName));
        }

        _context.Tutors.Add(tutor);
        await _context.SaveChangesAsync();
        return tutor;
    }

    private async Task<float[]> ReadVectorAsync(Guid tutorId)
    {
        await using var readContext = CreateContext();
        var row = await readContext.TutorEmbeddings.AsNoTracking().SingleAsync(e => e.TutorId == tutorId);
        return VectorMath.Deserialize(row.Embedding);
    }

    private async Task<bool> HasEmbeddingAsync(Guid tutorId)
    {
        await using var readContext = CreateContext();
        return await readContext.TutorEmbeddings.AnyAsync(e => e.TutorId == tutorId);
    }
}
