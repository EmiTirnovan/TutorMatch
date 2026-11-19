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
using TutorMatch.Api.Features.Tutors.UpdateTutor;
using TutorMatch.Api.Infrastructure.Persistence;
using TutorMatch.Tests.Common;
using TutorMatch.Tests.Features.SemanticSearch.Fakes;

namespace TutorMatch.Tests.Features.SemanticSearch;

/// <summary>
/// End-to-end: PUT /api/tutors/{id} commits the profile, the post-commit TutorSavedNotification regenerates
/// the embedding on a background task, and the next semantic search ranks the tutor by the new content.
/// </summary>
public class TutorEmbeddingRegenerationEndpointTests : IDisposable
{
    private static readonly TimeSpan BackgroundTimeout = TimeSpan.FromSeconds(10);

    private readonly WebApplicationFactory<Program> _factory;
    // Named shared-cache in-memory DB: the background embedding task and the test each use their own connection,
    // because a single SqliteConnection must not be used from two threads at once.
    private readonly string _connectionString = $"Data Source=regen-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private readonly SqliteConnection _connection;
    private readonly HttpClient _client;
    private readonly FakeOllamaClient _fakeOllama = new() { EmbeddingGenerator = DeterministicTextEmbedder.Embed };

    public TutorEmbeddingRegenerationEndpointTests()
    {
        _connection = new SqliteConnection(_connectionString);
        _connection.Open();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TutorMatchDbContext>));
                if (dbDescriptor != null) services.Remove(dbDescriptor);
                services.AddDbContext<TutorMatchDbContext>(options => options.UseSqlite(_connectionString));

                var ollamaDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IOllamaClient));
                if (ollamaDescriptor != null) services.Remove(ollamaDescriptor);
                services.AddSingleton<IOllamaClient>(_fakeOllama);
            });
        });

        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>().Database.EnsureCreated();

        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Given_TutorWithOldBio_When_UpdatingBioAndSubjects_Then_EmbeddingIsRegeneratedAndTutorIsFoundByNewContent()
    {
        var tutorId = await SeedIndexedTutorAsync("Elena Pop", "Predau mecanica clasica pentru liceu.", "Fizica");
        await SeedIndexedTutorAsync("Mihai Enache", "Predau baze de date si optimizare SQL.", "Baze de Date");
        var request = new UpdateTutorRequest(
            "Elena Pop", "Pregatesc studenti pentru astrofizica si cosmologie observationala.", 100m,
            new List<string> { "Fizica", "Astronomie" });
        var expectedText = "Elena Pop | Astronomie, Fizica | Pregatesc studenti pentru astrofizica si cosmologie observationala.";

        var updateResponse = await _client.PutAsJsonAsync($"/api/tutors/{tutorId}", request);
        var regenerated = await WaitForEmbeddingAsync(tutorId, DeterministicTextEmbedder.Embed(expectedText));
        var searchResponse = await _client.GetAsync("/api/tutors/semantic-search?q=astrofizica%20si%20cosmologie");

        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        regenerated.Should().BeTrue();
        _fakeOllama.CapturedPrompts.Should().Contain(expectedText);
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var search = await searchResponse.Content.ReadFromJsonAsync<SemanticSearchResponse>();
        search!.Mode.Should().Be("semantic");
        search.Results[0].TutorId.Should().Be(tutorId);
        search.Results[0].SimilarityScore.Should().BeGreaterThan(0f);
        search.Results[0].Bio.Should().Be(request.Bio);
    }

    [Fact]
    public async Task Given_TutorWithSameBio_When_ChangingOnlySubjects_Then_TutorIsFoundByTheNewSubject()
    {
        var tutorId = await SeedIndexedTutorAsync("Ion Popescu", "Profesor cu 10 ani experienta.", "Algebra");
        var request = new UpdateTutorRequest("Ion Popescu", "Profesor cu 10 ani experienta.", 90m, new List<string> { "Termodinamica" });
        var expectedText = "Ion Popescu | Termodinamica | Profesor cu 10 ani experienta.";

        var updateResponse = await _client.PutAsJsonAsync($"/api/tutors/{tutorId}", request);
        var regenerated = await WaitForEmbeddingAsync(tutorId, DeterministicTextEmbedder.Embed(expectedText));
        var search = await _client.GetFromJsonAsync<SemanticSearchResponse>("/api/tutors/semantic-search?q=termodinamica");

        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        regenerated.Should().BeTrue();
        search!.Mode.Should().Be("semantic");
        search.Results[0].TutorId.Should().Be(tutorId);
        search.Results[0].Subjects.Should().BeEquivalentTo(new[] { "Termodinamica" });
    }

    [Fact]
    public async Task Given_InvalidUpdate_When_UpdatingTutor_Then_ReturnsBadRequestAndKeepsPreviousEmbedding()
    {
        var tutorId = await SeedIndexedTutorAsync("Ana Stan", "Predau chimie organica.", "Chimie");
        var previousVector = await ReadVectorAsync(tutorId);
        var request = new UpdateTutorRequest("", "Bio noua", 90m);

        var response = await _client.PutAsJsonAsync($"/api/tutors/{tutorId}", request);
        await Task.Delay(200);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Tutor.Validation");
        (await ReadVectorAsync(tutorId)).Should().Equal(previousVector);
    }

    // Seeds a tutor directly in the database together with the embedding of its current profile text.
    private async Task<Guid> SeedIndexedTutorAsync(string name, string bio, string subjectName)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
        var subject = await db.Subjects.FirstOrDefaultAsync(s => s.Name == subjectName) ?? new Subject(subjectName);
        var tutor = new Tutor(name, $"{Guid.NewGuid():N}@test.ro", bio, 100m);
        tutor.AddSubject(subject);
        db.Tutors.Add(tutor);
        var vector = DeterministicTextEmbedder.Embed(TutorEmbeddingText.Build(name, new[] { subjectName }, bio));
        db.TutorEmbeddings.Add(new TutorEmbedding(tutor.Id, VectorMath.Serialize(vector)));
        await db.SaveChangesAsync();
        return tutor.Id;
    }

    private Task<bool> WaitForEmbeddingAsync(Guid tutorId, float[] expectedVector) =>
        Eventually.SatisfiedAsync(
            async () => (await ReadVectorAsync(tutorId)).SequenceEqual(expectedVector),
            BackgroundTimeout);

    private async Task<float[]> ReadVectorAsync(Guid tutorId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
        var row = await db.TutorEmbeddings.AsNoTracking().FirstOrDefaultAsync(e => e.TutorId == tutorId);
        return row is null ? Array.Empty<float>() : VectorMath.Deserialize(row.Embedding);
    }
}
