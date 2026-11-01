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
    private readonly SqliteConnection _connection;
    private readonly HttpClient _client;
    private readonly FakeOllamaClient _fakeOllama;

    public SemanticSearchEndpointTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
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
                    options.UseSqlite(_connection);
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
}

