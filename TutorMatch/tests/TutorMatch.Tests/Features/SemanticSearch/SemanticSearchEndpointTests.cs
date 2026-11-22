using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Features.SemanticSearch;
using TutorMatch.Api.Features.SemanticSearch.Services;
using TutorMatch.Api.Infrastructure.Persistence;
using TutorMatch.Tests.Features.SemanticSearch.Fakes;
using Xunit;

namespace TutorMatch.Tests.Features.SemanticSearch;

public class SemanticSearchEndpointTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    // Named shared-cache in-memory DB so concurrent requests each use their own connection.
    private readonly string _connectionString = $"Data Source=semantic-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private readonly SqliteConnection _connection;
    private readonly HttpClient _client;
    private readonly FakeOllamaClient _fakeOllama;

    public SemanticSearchEndpointTests()
    {
        _connection = new SqliteConnection(_connectionString);
        _connection.Open();

        _fakeOllama = new FakeOllamaClient();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Inlocuim DbContext cu conexiunea in-memory partajata
                var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TutorMatchDbContext>));
                if (dbDescriptor != null) services.Remove(dbDescriptor);

                services.AddDbContext<TutorMatchDbContext>(options =>
                {
                    options.UseSqlite(_connectionString);
                });

                // Inlocuim IOllamaClient cu fake-ul de test
                var ollamaDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IOllamaClient));
                if (ollamaDescriptor != null) services.Remove(ollamaDescriptor);

                services.AddSingleton<IOllamaClient>(_fakeOllama);
            });
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
        db.Database.EnsureCreated();

        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Given_OllamaIsHealthy_When_CallingSemanticSearchEndpoint_Then_ReturnsOkWithSemanticMode()
    {
        // Arrange
        var tutorId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var tutor = new Tutor("Tudor Matei", "tudor@test.ro", "Profesor pregatire analiza matematica.", 95m, tutorId);
            db.Tutors.Add(tutor);
            db.TutorEmbeddings.Add(new TutorEmbedding(tutorId, VectorMath.Serialize([1.0f, 0.0f])));
            await db.SaveChangesAsync();
        }

        _fakeOllama.EmbeddingGenerator = _ => [1.0f, 0.0f];

        // Act
        var response = await _client.GetAsync("/api/tutors/semantic-search?q=analiza");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SemanticSearchResponse>();
        result.Should().NotBeNull();
        result!.Mode.Should().Be("semantic");
        result.Results.Should().ContainSingle(r => r.TutorId == tutorId);
        result.Results[0].SimilarityScore.Should().BeApproximately(1.0f, 1e-4f);
    }

    [Fact]
    public async Task Given_OllamaFailsWithNetworkException_When_CallingSemanticSearchEndpoint_Then_ReturnsOkWithFallbackKeywordMode()
    {
        // Arrange
        var tutorId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var subject = new Subject("Informatica");
            db.Subjects.Add(subject);

            var tutor = new Tutor("Alex Radu", "alex@test.ro", "Meditatii programare", 100m, tutorId);
            tutor.AddSubject(subject);
            db.Tutors.Add(tutor);
            await db.SaveChangesAsync();
        }

        // Simulam caderea retelei catre Ollama
        _fakeOllama.ExceptionToThrow = new HttpRequestException("Connection refused by Ollama host.");

        // Act
        var response = await _client.GetAsync("/api/tutors/semantic-search?q=Informatica");

        // Assert - Asiguram ca nu se returneaza HTTP 500, ci 200 OK cu fallback
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SemanticSearchResponse>();
        result.Should().NotBeNull();
        result!.Mode.Should().Be("keyword");
        result.Message.Should().NotBeNullOrWhiteSpace();
        result.Results.Should().ContainSingle(r => r.TutorId == tutorId);
    }

    [Fact]
    public async Task Given_IdenticalQueryTwice_When_CallingSemanticSearchEndpoint_Then_SecondResponseComesFromCacheWithoutCallingOllama()
    {
        var tutorId = await SeedTutorWithEmbeddingAsync("Profesor pregatire analiza matematica.", [1.0f, 0.0f]);
        _fakeOllama.EmbeddingGenerator = _ => [1.0f, 0.0f];
        var firstResponse = await _client.GetAsync("/api/tutors/semantic-search?q=analiza%20matematica");

        var secondResponse = await _client.GetAsync("/api/tutors/semantic-search?q=%20%20Analiza%20Matematica%20");

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var second = await secondResponse.Content.ReadFromJsonAsync<SemanticSearchResponse>();
        second!.Mode.Should().Be("semantic");
        second.Results.Should().ContainSingle(r => r.TutorId == tutorId);
        _fakeOllama.CallCount.Should().Be(1);
        (await EmbeddingExistsAsync(tutorId)).Should().BeTrue();
    }

    [Fact]
    public async Task Given_OllamaSlowerThan3500ms_When_CallingSemanticSearchEndpoint_Then_ReturnsOkWithKeywordModeWithinBudget()
    {
        var tutorId = await SeedTutorWithSubjectAsync("Informatica");
        _fakeOllama.Delay = TimeSpan.FromSeconds(10);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var response = await _client.GetAsync("/api/tutors/semantic-search?q=Informatica");
        stopwatch.Stop();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SemanticSearchResponse>();
        result!.Mode.Should().Be("keyword");
        result.Message.Should().Be(SemanticSearchQueryHandler.FallbackMessage);
        result.Results.Should().ContainSingle(r => r.TutorId == tutorId);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
        (await EmbeddingExistsAsync(tutorId)).Should().BeFalse();
    }

    [Fact]
    public async Task Given_15RequestsAlreadyServedThisMinute_When_Sending16thRequest_Then_Returns429WithErrorBody()
    {
        var tutorId = await SeedTutorWithEmbeddingAsync("Profesor de fizica.", [1.0f, 0.0f]);
        _fakeOllama.EmbeddingGenerator = _ => [1.0f, 0.0f];
        var allowed = await Task.WhenAll(Enumerable.Range(0, SemanticSearchRateLimiting.PermitLimit)
            .Select(_ => _client.GetAsync("/api/tutors/semantic-search?q=fizica")));

        var rejected = await _client.GetAsync("/api/tutors/semantic-search?q=fizica");

        allowed.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);
        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var body = await rejected.Content.ReadAsStringAsync();
        body.Should().Contain(SemanticSearchRateLimiting.RejectionCode);
        (await EmbeddingExistsAsync(tutorId)).Should().BeTrue();
    }

    [Fact]
    public async Task Given_RateLimitExhaustedOnSemanticSearch_When_CallingKeywordSearch_Then_KeywordSearchIsNotLimited()
    {
        await Task.WhenAll(Enumerable.Range(0, SemanticSearchRateLimiting.PermitLimit + 1)
            .Select(_ => _client.GetAsync("/api/tutors/semantic-search?q=fizica")));

        var response = await _client.GetAsync("/api/tutors/search?subject=Fizica");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<Guid> SeedTutorWithEmbeddingAsync(string bio, float[] vector)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
        var tutor = new Tutor("Tudor Matei", $"{Guid.NewGuid():N}@test.ro", bio, 95m);
        db.Tutors.Add(tutor);
        db.TutorEmbeddings.Add(new TutorEmbedding(tutor.Id, VectorMath.Serialize(vector)));
        await db.SaveChangesAsync();
        return tutor.Id;
    }

    private async Task<Guid> SeedTutorWithSubjectAsync(string subjectName)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
        var tutor = new Tutor("Alex Radu", $"{Guid.NewGuid():N}@test.ro", "Meditatii programare", 100m);
        tutor.AddSubject(new Subject(subjectName));
        db.Tutors.Add(tutor);
        await db.SaveChangesAsync();
        return tutor.Id;
    }

    private async Task<bool> EmbeddingExistsAsync(Guid tutorId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
        return await db.TutorEmbeddings.AnyAsync(e => e.TutorId == tutorId);
    }
}

