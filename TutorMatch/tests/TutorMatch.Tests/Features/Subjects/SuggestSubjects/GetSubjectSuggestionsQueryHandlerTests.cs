using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Features.Subjects.SuggestSubjects;
using TutorMatch.Api.Infrastructure.Persistence;
using Xunit;

namespace TutorMatch.Tests.Features.Subjects.SuggestSubjects;

public class GetSubjectSuggestionsQueryHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TutorMatchDbContext _dbContext;
    private readonly GetSubjectSuggestionsQueryHandler _handler;

    public GetSubjectSuggestionsQueryHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TutorMatchDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new TutorMatchDbContext(options);
        _dbContext.Database.EnsureCreated();

        _handler = new GetSubjectSuggestionsQueryHandler(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Given_EmptyOrNullOrWhitespaceQuery_When_Suggesting_Then_ReturnsEmptyList()
    {
        await SeedSubject("Algorithms", 10);

        var nullResult = await _handler.HandleAsync(new GetSubjectSuggestionsQuery(null));
        var emptyResult = await _handler.HandleAsync(new GetSubjectSuggestionsQuery(""));
        var whitespaceResult = await _handler.HandleAsync(new GetSubjectSuggestionsQuery("   "));

        nullResult.IsSuccess.Should().BeTrue();
        nullResult.Value.Suggestions.Should().BeEmpty();

        emptyResult.IsSuccess.Should().BeTrue();
        emptyResult.Value.Suggestions.Should().BeEmpty();

        whitespaceResult.IsSuccess.Should().BeTrue();
        whitespaceResult.Value.Suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_MatchingQuery_When_Suggesting_Then_ReturnsMatchingSubjects()
    {
        await SeedSubject("Java", 5);
        await SeedSubject("JavaScript", 3);
        await SeedSubject("Python", 10);

        var result = await _handler.HandleAsync(new GetSubjectSuggestionsQuery("Java"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Suggestions.Should().Contain("Java");
        result.Value.Suggestions.Should().Contain("JavaScript");
        result.Value.Suggestions.Should().NotContain("Python");
    }

    [Fact]
    public async Task Given_DifferentCasing_When_Suggesting_Then_PerformsCaseInsensitiveMatch()
    {
        await SeedSubject("Machine Learning", 5);

        var lowerResult = await _handler.HandleAsync(new GetSubjectSuggestionsQuery("machine"));
        var upperResult = await _handler.HandleAsync(new GetSubjectSuggestionsQuery("MACHINE"));
        var mixedResult = await _handler.HandleAsync(new GetSubjectSuggestionsQuery("mAcHiNe"));

        lowerResult.Value.Suggestions.Should().ContainSingle().Which.Should().Be("Machine Learning");
        upperResult.Value.Suggestions.Should().ContainSingle().Which.Should().Be("Machine Learning");
        mixedResult.Value.Suggestions.Should().ContainSingle().Which.Should().Be("Machine Learning");
    }

    [Fact]
    public async Task Given_SubstringQuery_When_Suggesting_Then_MatchesSubstring()
    {
        await SeedSubject("Object-Oriented Programming", 4);

        var result = await _handler.HandleAsync(new GetSubjectSuggestionsQuery("Oriented"));

        result.Value.Suggestions.Should().Contain("Object-Oriented Programming");
    }

    [Fact]
    public async Task Given_PrefixQuery_When_Suggesting_Then_MatchesPrefix()
    {
        await SeedSubject("Algorithms and Data Structures", 8);

        var result = await _handler.HandleAsync(new GetSubjectSuggestionsQuery("Algo"));

        result.Value.Suggestions.Should().Contain("Algorithms and Data Structures");
    }

    [Fact]
    public async Task Given_MultipleMatches_When_Suggesting_Then_OrdersByUsageCountDescending()
    {
        await SeedSubject("Data Mining", 2);
        await SeedSubject("Data Structures", 10);
        await SeedSubject("Database Systems", 5);

        var result = await _handler.HandleAsync(new GetSubjectSuggestionsQuery("Data"));

        result.Value.Suggestions.Should().HaveCount(3);
        result.Value.Suggestions[0].Should().Be("Data Structures"); // Usage 10
        result.Value.Suggestions[1].Should().Be("Database Systems"); // Usage 5
        result.Value.Suggestions[2].Should().Be("Data Mining");     // Usage 2
    }

    [Fact]
    public async Task Given_EqualUsageCount_When_Suggesting_Then_OrdersAlphabeticallySecondary()
    {
        await SeedSubject("Calculus B", 3);
        await SeedSubject("Calculus A", 3);
        await SeedSubject("Calculus C", 3);

        var result = await _handler.HandleAsync(new GetSubjectSuggestionsQuery("Calculus"));

        result.Value.Suggestions.Should().Equal("Calculus A", "Calculus B", "Calculus C");
    }

    [Fact]
    public async Task Given_MoreThanThreeMatches_When_Suggesting_Then_ReturnsMaximumThreeResults()
    {
        await SeedSubject("Web Development Frontend", 1);
        await SeedSubject("Web Development Backend", 2);
        await SeedSubject("Web Development Fullstack", 3);
        await SeedSubject("Web Security", 4);
        await SeedSubject("Web APIs", 5);

        var result = await _handler.HandleAsync(new GetSubjectSuggestionsQuery("Web"));

        result.Value.Suggestions.Should().HaveCount(3);
        result.Value.Suggestions.Should().Equal("Web APIs", "Web Security", "Web Development Fullstack");
    }

    [Fact]
    public async Task Given_UnknownQuery_When_Suggesting_Then_ReturnsEmptyList()
    {
        await SeedSubject("Computer Networks", 5);

        var result = await _handler.HandleAsync(new GetSubjectSuggestionsQuery("NonexistentSubject123"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Suggestions.Should().BeEmpty();
    }

    private async Task SeedSubject(string name, int usageCount)
    {
        var subject = new Subject(name, usageCount: usageCount);
        _dbContext.Subjects.Add(subject);
        await _dbContext.SaveChangesAsync();
    }
}
