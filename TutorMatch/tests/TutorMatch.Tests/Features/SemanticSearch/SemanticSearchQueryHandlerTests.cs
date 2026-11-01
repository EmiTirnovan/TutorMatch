using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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
}

