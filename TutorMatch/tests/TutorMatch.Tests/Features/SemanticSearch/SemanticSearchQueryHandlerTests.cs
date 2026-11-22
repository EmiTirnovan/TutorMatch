using System.Diagnostics;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Features.Discovery.SearchTutors;
using TutorMatch.Api.Features.SemanticSearch;
using TutorMatch.Api.Features.SemanticSearch.Services;
using TutorMatch.Api.Infrastructure.Persistence;
using TutorMatch.Tests.Features.SemanticSearch.Fakes;
using Xunit;

namespace TutorMatch.Tests.Features.SemanticSearch;

public class SemanticSearchQueryHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TutorMatchDbContext _context;
    private readonly FakeOllamaClient _fakeOllama;
    private readonly SearchTutorsQueryHandler _fallbackHandler;

    public SemanticSearchQueryHandlerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TutorMatchDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new TutorMatchDbContext(options);
        _context.Database.EnsureCreated();

        _fakeOllama = new FakeOllamaClient();
        _fallbackHandler = new SearchTutorsQueryHandler(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Given_OllamaSucceeds_When_SearchingSemantically_Then_ReturnsTopTutorsOrderedBySimilarityWithSemanticMode()
    {
        // Arrange
        var tutor1 = new Tutor("Ion Popescu", "ion@test.ro", "Profesor de analiza matematica.", 90m);
        var tutor2 = new Tutor("Alex Radu", "alex@test.ro", "Inginer software si mentor algoritmi.", 110m);
        var tutor3 = new Tutor("Elena Dumitrescu", "elena@test.ro", "Cercetator fizica cuantica.", 100m);

        _context.Tutors.AddRange(tutor1, tutor2, tutor3);
        await _context.SaveChangesAsync();

        // Vector interogare student: [1, 0]
        float[] queryVector = [1.0f, 0.0f];
        _fakeOllama.EmbeddingGenerator = _ => queryVector;

        // Tutor 1: Identic cu interogarea -> cosinus = 1.0
        // Tutor 2: La 45 grade -> cosinus = ~0.707
        // Tutor 3: Ortogonal -> cosinus = 0.0
        _context.TutorEmbeddings.AddRange(
            new TutorEmbedding(tutor1.Id, VectorMath.Serialize([1.0f, 0.0f])),
            new TutorEmbedding(tutor2.Id, VectorMath.Serialize([0.7071f, 0.7071f])),
            new TutorEmbedding(tutor3.Id, VectorMath.Serialize([0.0f, 1.0f])));
        await _context.SaveChangesAsync();

        var handler = new SemanticSearchQueryHandler(_fakeOllama, _context, _fallbackHandler);

        // Act
        var result = await handler.HandleAsync(new SemanticSearchQuery("analiza matematica"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Mode.Should().Be("semantic");
        result.Value.Results.Should().HaveCount(3);

        // Ordinea trebuie sa fie descrescatoare dupa similaritate
        result.Value.Results[0].TutorId.Should().Be(tutor1.Id);
        result.Value.Results[0].SimilarityScore.Should().BeApproximately(1.0f, 1e-4f);

        result.Value.Results[1].TutorId.Should().Be(tutor2.Id);
        result.Value.Results[1].SimilarityScore.Should().BeApproximately(0.7071f, 1e-4f);

        result.Value.Results[2].TutorId.Should().Be(tutor3.Id);
        result.Value.Results[2].SimilarityScore.Should().BeApproximately(0.0f, 1e-4f);
    }

    [Fact]
    public async Task Given_OllamaTimesOut_When_SearchingSemantically_Then_CatchesExceptionAndReturnsFallbackWithKeywordMode()
    {
        // Arrange
        var subject = new Subject("Informatica");
        _context.Subjects.Add(subject);

        var tutor = new Tutor("Alex Radu", "alex@test.ro", "Mentor C++", 100m);
        tutor.AddSubject(subject);
        _context.Tutors.Add(tutor);
        await _context.SaveChangesAsync();

        // Simulam caderea/timeout-ul Ollama
        _fakeOllama.ExceptionToThrow = new TimeoutException("Ollama request timed out after 5 seconds.");

        var handler = new SemanticSearchQueryHandler(_fakeOllama, _context, _fallbackHandler);

        // Act
        var result = await handler.HandleAsync(new SemanticSearchQuery("Informatica"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Mode.Should().Be("keyword");
        result.Value.Message.Should().NotBeNullOrWhiteSpace();
        result.Value.Results.Should().ContainSingle(r => r.TutorId == tutor.Id);
    }

    [Fact]
    public async Task Given_OllamaReturnsNullVector_When_SearchingSemantically_Then_TriggersFallbackGracefully()
    {
        // Arrange
        var subject = new Subject("Matematica");
        _context.Subjects.Add(subject);

        var tutor = new Tutor("Ion Popescu", "ion@test.ro", "Profesor", 80m);
        tutor.AddSubject(subject);
        _context.Tutors.Add(tutor);
        await _context.SaveChangesAsync();

        // Ollama returneaza null (ex. eroare HTTP 500 gestionata de client)
        _fakeOllama.EmbeddingGenerator = _ => null;

        var handler = new SemanticSearchQueryHandler(_fakeOllama, _context, _fallbackHandler);

        // Act
        var result = await handler.HandleAsync(new SemanticSearchQuery("Matematica"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Mode.Should().Be("keyword");
        result.Value.Results.Should().ContainSingle(r => r.TutorId == tutor.Id);
    }

    [Fact]
    public async Task Given_OllamaUnavailable_When_FallingBack_Then_ReturnsBioAndAllSubjectsOfMatchingTutor()
    {
        var math = new Subject("Matematica");
        var physics = new Subject("Fizica");
        _context.Subjects.AddRange(math, physics);
        var tutor = new Tutor("Ion Popescu", "ion@test.ro", "Profesor de matematica", 80m);
        tutor.AddSubject(math);
        tutor.AddSubject(physics);
        _context.Tutors.Add(tutor);
        await _context.SaveChangesAsync();
        _fakeOllama.EmbeddingGenerator = _ => null;
        var handler = new SemanticSearchQueryHandler(_fakeOllama, _context, _fallbackHandler);

        var result = await handler.HandleAsync(new SemanticSearchQuery("Matematica"));

        var item = result.Value.Results.Single();
        item.Bio.Should().Be("Profesor de matematica");
        item.HourlyRate.Should().Be(80m);
        item.Subjects.Should().BeEquivalentTo(["Matematica", "Fizica"]);
    }

    [Fact]
    public async Task Given_OllamaUnavailableAndNoMatch_When_FallingBack_Then_ReturnsEmptyKeywordResults()
    {
        var subject = new Subject("Matematica");
        _context.Subjects.Add(subject);
        var tutor = new Tutor("Ion Popescu", "ion@test.ro", "Profesor", 80m);
        tutor.AddSubject(subject);
        _context.Tutors.Add(tutor);
        await _context.SaveChangesAsync();
        _fakeOllama.EmbeddingGenerator = _ => null;
        var handler = new SemanticSearchQueryHandler(_fakeOllama, _context, _fallbackHandler);

        var result = await handler.HandleAsync(new SemanticSearchQuery("Chimie"));

        result.Value.Mode.Should().Be("keyword");
        result.Value.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_EmptyQuery_When_SearchingSemantically_Then_ReturnsEmptyResultsWithSemanticMode()
    {
        // Arrange
        var handler = new SemanticSearchQueryHandler(_fakeOllama, _context, _fallbackHandler);

        // Act
        var result = await handler.HandleAsync(new SemanticSearchQuery("   "));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Mode.Should().Be("semantic");
        result.Value.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_NoTutorsInDatabase_When_SearchingSemantically_Then_ReturnsEmptyResultsWithSemanticMode()
    {
        // Arrange
        _fakeOllama.EmbeddingGenerator = _ => [0.1f, 0.2f];
        var handler = new SemanticSearchQueryHandler(_fakeOllama, _context, _fallbackHandler);

        // Act
        var result = await handler.HandleAsync(new SemanticSearchQuery("Fizica"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Mode.Should().Be("semantic");
        result.Value.Results.Should().BeEmpty();
    }

    [Theory]
    [InlineData("  Analiza   Matematica ", "analiza matematica")]
    [InlineData("ALGEBRA\tliniara\n", "algebra liniara")]
    [InlineData("fizica", "fizica")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void Given_RawQuery_When_Normalizing_Then_TrimsCollapsesWhitespaceAndLowercases(string? raw, string expected)
    {
        var normalized = SemanticSearchQueryHandler.NormalizeQuery(raw);

        normalized.Should().Be(expected);
    }

    [Fact]
    public async Task Given_IdenticalQueryTwice_When_Searching_Then_SecondCallHitsCacheWithoutCallingOllama()
    {
        await SeedTutorWithEmbeddingAsync([1.0f, 0.0f]);
        _fakeOllama.EmbeddingGenerator = _ => [1.0f, 0.0f];
        var handler = CreateCachingHandler(new MemoryCache(new MemoryCacheOptions()));
        await handler.HandleAsync(new SemanticSearchQuery("analiza matematica"));

        var second = await handler.HandleAsync(new SemanticSearchQuery("analiza matematica"));

        _fakeOllama.CallCount.Should().Be(1);
        second.Value.Mode.Should().Be(SemanticSearchQueryHandler.ModeSemantic);
        second.Value.Results.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_SlowOllamaAndWarmCache_When_RepeatingQuery_Then_RespondsInUnder15Milliseconds()
    {
        await SeedTutorWithEmbeddingAsync([1.0f, 0.0f]);
        _fakeOllama.EmbeddingGenerator = _ => [1.0f, 0.0f];
        _fakeOllama.Delay = TimeSpan.FromMilliseconds(300);
        var handler = CreateCachingHandler(new MemoryCache(new MemoryCacheOptions()));
        await handler.HandleAsync(new SemanticSearchQuery("analiza matematica"));
        var stopwatch = Stopwatch.StartNew();

        await handler.HandleAsync(new SemanticSearchQuery("analiza matematica"));
        stopwatch.Stop();

        _fakeOllama.CallCount.Should().Be(1);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(15));
    }

    [Fact]
    public async Task Given_QueriesDifferingOnlyInCaseAndWhitespace_When_Searching_Then_ShareOneNormalizedCacheEntry()
    {
        _fakeOllama.EmbeddingGenerator = _ => [1.0f, 0.0f];
        var handler = CreateCachingHandler(new MemoryCache(new MemoryCacheOptions()));
        await handler.HandleAsync(new SemanticSearchQuery("  Analiza   MATEMATICA "));

        await handler.HandleAsync(new SemanticSearchQuery("analiza matematica"));

        _fakeOllama.CallCount.Should().Be(1);
        _fakeOllama.CapturedPrompts.Should().ContainSingle().Which.Should().Be("analiza matematica");
    }

    [Fact]
    public async Task Given_FirstOllamaCallFails_When_RepeatingQuery_Then_FailureIsNotCached()
    {
        var calls = 0;
        _fakeOllama.EmbeddingGenerator = _ => Interlocked.Increment(ref calls) == 1 ? null : [1.0f, 0.0f];
        var handler = CreateCachingHandler(new MemoryCache(new MemoryCacheOptions()));
        var first = await handler.HandleAsync(new SemanticSearchQuery("fizica"));

        var second = await handler.HandleAsync(new SemanticSearchQuery("fizica"));

        first.Value.Mode.Should().Be(SemanticSearchQueryHandler.ModeKeyword);
        second.Value.Mode.Should().Be(SemanticSearchQueryHandler.ModeSemantic);
        _fakeOllama.CallCount.Should().Be(2);
    }

    [Theory]
    [InlineData(29, 1)]
    [InlineData(31, 2)]
    public async Task Given_CachedQueryEmbedding_When_TimeAdvances_Then_EntryLivesThirtyMinutes(int minutesLater, int expectedOllamaCalls)
    {
        var clock = new ManualSystemClock(DateTimeOffset.UtcNow);
        var cache = new MemoryCache(new MemoryCacheOptions { Clock = clock });
        _fakeOllama.EmbeddingGenerator = _ => [1.0f, 0.0f];
        var handler = CreateCachingHandler(cache);
        await handler.HandleAsync(new SemanticSearchQuery("fizica"));
        clock.UtcNow = clock.UtcNow.AddMinutes(minutesLater);

        await handler.HandleAsync(new SemanticSearchQuery("fizica"));

        _fakeOllama.CallCount.Should().Be(expectedOllamaCalls);
    }

    [Fact]
    public async Task Given_HandlersForDifferentModelsSharingCache_When_SearchingSameQuery_Then_EachModelEmbedsSeparately()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        _fakeOllama.EmbeddingGenerator = _ => [1.0f, 0.0f];
        var bgeHandler = CreateCachingHandler(cache, new OllamaOptions { Model = "bge-m3" });
        var nomicHandler = CreateCachingHandler(cache, new OllamaOptions { Model = "nomic-embed-text" });
        await bgeHandler.HandleAsync(new SemanticSearchQuery("fizica"));

        await nomicHandler.HandleAsync(new SemanticSearchQuery("fizica"));

        _fakeOllama.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task Given_OllamaSlowerThanDefault3500msBudget_When_Searching_Then_FallsBackToKeywordAtTheBudget()
    {
        var tutor = await SeedTutorWithSubjectAsync("Informatica");
        _fakeOllama.Delay = TimeSpan.FromSeconds(10);
        var handler = CreateCachingHandler(new MemoryCache(new MemoryCacheOptions()));
        var stopwatch = Stopwatch.StartNew();

        var result = await handler.HandleAsync(new SemanticSearchQuery("Informatica"));
        stopwatch.Stop();

        result.IsSuccess.Should().BeTrue();
        result.Value.Mode.Should().Be(SemanticSearchQueryHandler.ModeKeyword);
        result.Value.Message.Should().Be(SemanticSearchQueryHandler.FallbackMessage);
        result.Value.Results.Should().ContainSingle(r => r.TutorId == tutor.Id);
        stopwatch.Elapsed.Should().BeGreaterThan(TimeSpan.FromMilliseconds(3400));
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(5000));
    }

    [Fact]
    public async Task Given_ClientIgnoringCancellation_When_BudgetElapses_Then_FallsBackToKeywordAnyway()
    {
        var hangingClient = new HangingOllamaClient();
        var options = Options.Create(new OllamaOptions { QueryTimeoutMilliseconds = 100 });
        var handler = new SemanticSearchQueryHandler(hangingClient, _context, _fallbackHandler, new MemoryCache(new MemoryCacheOptions()), options);
        var stopwatch = Stopwatch.StartNew();

        var result = await handler.HandleAsync(new SemanticSearchQuery("Informatica"));
        stopwatch.Stop();

        result.Value.Mode.Should().Be(SemanticSearchQueryHandler.ModeKeyword);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Given_OllamaThrowsInvalidOperation_When_Searching_Then_ReturnsKeywordModeWithoutFailure()
    {
        _fakeOllama.ExceptionToThrow = new InvalidOperationException("Model not loaded.");
        var handler = CreateCachingHandler(new MemoryCache(new MemoryCacheOptions()));

        var result = await handler.HandleAsync(new SemanticSearchQuery("Informatica"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Mode.Should().Be(SemanticSearchQueryHandler.ModeKeyword);
    }

    [Fact]
    public async Task Given_CallerCancelsRequest_When_Searching_Then_PropagatesCancellationInsteadOfFallingBack()
    {
        _fakeOllama.Delay = TimeSpan.FromSeconds(10);
        var handler = CreateCachingHandler(new MemoryCache(new MemoryCacheOptions()));
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        var act = () => handler.HandleAsync(new SemanticSearchQuery("Informatica"), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private SemanticSearchQueryHandler CreateCachingHandler(IMemoryCache cache, OllamaOptions? options = null) =>
        new(_fakeOllama, _context, _fallbackHandler, cache, Options.Create(options ?? new OllamaOptions()));

    private async Task<Tutor> SeedTutorWithEmbeddingAsync(float[] vector)
    {
        var tutor = new Tutor("Ion Popescu", $"{Guid.NewGuid():N}@test.ro", "Profesor de analiza matematica.", 90m);
        _context.Tutors.Add(tutor);
        _context.TutorEmbeddings.Add(new TutorEmbedding(tutor.Id, VectorMath.Serialize(vector)));
        await _context.SaveChangesAsync();
        return tutor;
    }

    private async Task<Tutor> SeedTutorWithSubjectAsync(string subjectName)
    {
        var tutor = new Tutor("Alex Radu", $"{Guid.NewGuid():N}@test.ro", "Mentor C++", 100m);
        tutor.AddSubject(new Subject(subjectName));
        _context.Tutors.Add(tutor);
        await _context.SaveChangesAsync();
        return tutor;
    }

    private sealed class ManualSystemClock : ISystemClock
    {
        public ManualSystemClock(DateTimeOffset start)
        {
            UtcNow = start;
        }

        public DateTimeOffset UtcNow { get; set; }
    }

    private sealed class HangingOllamaClient : IOllamaClient
    {
        public Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default) =>
            new TaskCompletionSource<float[]?>().Task;
    }
}

