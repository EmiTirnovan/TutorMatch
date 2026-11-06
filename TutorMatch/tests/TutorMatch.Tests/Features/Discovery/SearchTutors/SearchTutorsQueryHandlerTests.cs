using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Domain.Students;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Features.Discovery.SearchTutors;
using TutorMatch.Api.Infrastructure.Persistence;
using Xunit;

namespace TutorMatch.Tests.Features.Discovery.SearchTutors;

public class SearchTutorsQueryHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TutorMatchDbContext _context;
    private readonly SearchTutorsQueryHandler _handler;

    public SearchTutorsQueryHandlerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TutorMatchDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new TutorMatchDbContext(options);
        _context.Database.EnsureCreated();

        _handler = new SearchTutorsQueryHandler(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private async Task<Tutor> SeedTutorAsync(
        string name,
        decimal hourlyRate,
        string[]? subjects = null,
        DateRange[]? availability = null)
    {
        var tutor = new Tutor(name, $"{Guid.NewGuid():N}@test.com", "Bio", hourlyRate);

        foreach (var subjectName in subjects ?? [])
        {
            tutor.AddSubject(new Subject(subjectName));
        }

        foreach (var range in availability ?? [])
        {
            tutor.AddAvailability(range);
        }

        _context.Tutors.Add(tutor);
        await _context.SaveChangesAsync();
        return tutor;
    }

    private async Task SeedPriceLadderAsync()
    {
        await SeedTutorAsync("Tutor 120", 120m);
        await SeedTutorAsync("Tutor 50", 50m);
        await SeedTutorAsync("Tutor 80", 80m);
    }

    private async Task SeedRatingsAsync(Tutor tutor, params int[] scores)
    {
        var start = new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc);

        foreach (var score in scores)
        {
            var student = new Student(Guid.NewGuid(), "Student", $"{Guid.NewGuid():N}@test.com");
            var session = new Session(
                Guid.NewGuid(),
                tutor.Id,
                student.Id,
                new DateRange(start, start.AddHours(1)),
                SessionStatus.Completed);

            _context.Students.Add(student);
            _context.Sessions.Add(session);
            _context.Ratings.Add(new Rating(session.Id, tutor.Id, student.Id, score));
        }

        await _context.SaveChangesAsync();
    }

    private static DateRange Slot(int day, int startHour, int endHour)
    {
        return new DateRange(
            new DateTime(2026, 11, day, startHour, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, day, endHour, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Given_NoFilters_When_Searching_Then_ReturnsAllTutorsByAscendingPriceInKeywordMode()
    {
        await SeedPriceLadderAsync();

        var result = await _handler.HandleAsync(new SearchTutorsQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Mode.Should().Be("keyword");
        result.Value.TotalCount.Should().Be(3);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
        result.Value.Tutors.Select(t => t.Name).Should().Equal("Tutor 50", "Tutor 80", "Tutor 120");
    }

    [Fact]
    public async Task Given_NoTutors_When_Searching_Then_ReturnsEmptyResult()
    {
        var result = await _handler.HandleAsync(new SearchTutorsQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Tutors.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_Tutor_When_Searching_Then_MapsIdNameAndHourlyRate()
    {
        var tutor = await SeedTutorAsync("Maria Dragomir", 75m);

        var result = await _handler.HandleAsync(new SearchTutorsQuery());

        var item = result.Value.Tutors.Single();
        item.Id.Should().Be(tutor.Id);
        item.Name.Should().Be("Maria Dragomir");
        item.HourlyRate.Should().Be(75m);
    }

    [Fact]
    public async Task Given_MinPrice_When_Searching_Then_ReturnsTutorsAtOrAboveMinPrice()
    {
        await SeedPriceLadderAsync();

        var result = await _handler.HandleAsync(new SearchTutorsQuery(MinPrice: 80m));

        result.Value.Tutors.Select(t => t.Name).Should().Equal("Tutor 80", "Tutor 120");
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Given_MaxPrice_When_Searching_Then_ReturnsTutorsAtOrBelowMaxPrice()
    {
        await SeedPriceLadderAsync();

        var result = await _handler.HandleAsync(new SearchTutorsQuery(MaxPrice: 80m));

        result.Value.Tutors.Select(t => t.Name).Should().Equal("Tutor 50", "Tutor 80");
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Given_MinAndMaxPrice_When_Searching_Then_ReturnsOnlyTutorsInsideRange()
    {
        await SeedPriceLadderAsync();

        var result = await _handler.HandleAsync(new SearchTutorsQuery(MinPrice: 60m, MaxPrice: 100m));

        result.Value.Tutors.Select(t => t.Name).Should().Equal("Tutor 80");
    }

    [Fact]
    public async Task Given_SubjectFragment_When_Searching_Then_ReturnsTutorsTeachingMatchingSubject()
    {
        await SeedTutorAsync("Tutor Mate", 50m, subjects: ["Matematica"]);
        await SeedTutorAsync("Tutor Info", 150m, subjects: ["Informatica"]);

        var result = await _handler.HandleAsync(new SearchTutorsQuery(Subject: "Mate"));

        result.Value.Tutors.Select(t => t.Name).Should().Equal("Tutor Mate");
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Given_SubjectWithoutMatch_When_Searching_Then_ReturnsEmptyResult()
    {
        await SeedTutorAsync("Tutor Mate", 50m, subjects: ["Matematica"]);

        var result = await _handler.HandleAsync(new SearchTutorsQuery(Subject: "Chimie"));

        result.Value.Tutors.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Given_BlankSubject_When_Searching_Then_IgnoresSubjectFilter(string? subject)
    {
        await SeedTutorAsync("Tutor Mate", 50m, subjects: ["Matematica"]);
        await SeedTutorAsync("Tutor Fara Materii", 80m);

        var result = await _handler.HandleAsync(new SearchTutorsQuery(Subject: subject));

        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Given_TutorWithSeveralMatchingSubjects_When_Searching_Then_ReturnsTutorOnce()
    {
        await SeedTutorAsync("Tutor Mate", 50m, subjects: ["Matematica", "Matematica Aplicata"]);

        var result = await _handler.HandleAsync(new SearchTutorsQuery(Subject: "Mate"));

        result.Value.Tutors.Should().ContainSingle();
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Given_DateInsideAvailability_When_Searching_Then_ReturnsOnlyAvailableTutors()
    {
        await SeedTutorAsync("Available On 10th", 50m, availability: [Slot(10, 10, 12)]);
        await SeedTutorAsync("Available On 12th", 60m, availability: [Slot(12, 10, 12)]);

        var result = await _handler.HandleAsync(new SearchTutorsQuery(Date: new DateTime(2026, 11, 10)));

        result.Value.Tutors.Select(t => t.Name).Should().Equal("Available On 10th");
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Given_DateWithTimeOfDay_When_Searching_Then_MatchesByCalendarDay()
    {
        await SeedTutorAsync("Available On 10th", 50m, availability: [Slot(10, 8, 9)]);

        var result = await _handler.HandleAsync(new SearchTutorsQuery(Date: new DateTime(2026, 11, 10, 23, 30, 0)));

        result.Value.Tutors.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_AvailabilitySpanningSeveralDays_When_SearchingMiddleDay_Then_ReturnsTutor()
    {
        var overnight = new DateRange(
            new DateTime(2026, 11, 10, 22, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 12, 2, 0, 0, DateTimeKind.Utc));
        await SeedTutorAsync("Long Availability", 50m, availability: [overnight]);

        var result = await _handler.HandleAsync(new SearchTutorsQuery(Date: new DateTime(2026, 11, 11)));

        result.Value.Tutors.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_DateOutsideAvailability_When_Searching_Then_ReturnsEmptyResult()
    {
        await SeedTutorAsync("Available On 10th", 50m, availability: [Slot(10, 10, 12)]);

        var result = await _handler.HandleAsync(new SearchTutorsQuery(Date: new DateTime(2026, 11, 11)));

        result.Value.Tutors.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_SubjectAndMaxPrice_When_Searching_Then_AppliesBothFilters()
    {
        await SeedTutorAsync("Cheap Math", 50m, subjects: ["Matematica"]);
        await SeedTutorAsync("Expensive Math", 150m, subjects: ["Matematica"]);
        await SeedTutorAsync("Cheap Info", 40m, subjects: ["Informatica"]);

        var result = await _handler.HandleAsync(new SearchTutorsQuery(Subject: "Matematica", MaxPrice: 100m));

        result.Value.Tutors.Select(t => t.Name).Should().Equal("Cheap Math");
    }

    [Theory]
    [InlineData("priceDesc")]
    [InlineData("PRICEDESC")]
    public async Task Given_DescendingSort_When_Searching_Then_OrdersByPriceDescending(string sortBy)
    {
        await SeedPriceLadderAsync();

        var result = await _handler.HandleAsync(new SearchTutorsQuery(SortBy: sortBy));

        result.Value.Tutors.Select(t => t.Name).Should().Equal("Tutor 120", "Tutor 80", "Tutor 50");
    }

    [Theory]
    [InlineData("priceAsc")]
    [InlineData("PRICEASC")]
    [InlineData("unknown")]
    [InlineData(null)]
    public async Task Given_AscendingOrUnknownSort_When_Searching_Then_OrdersByPriceAscending(string? sortBy)
    {
        await SeedPriceLadderAsync();

        var result = await _handler.HandleAsync(new SearchTutorsQuery(SortBy: sortBy));

        result.Value.Tutors.Select(t => t.Name).Should().Equal("Tutor 50", "Tutor 80", "Tutor 120");
    }

    [Fact]
    public async Task Given_SecondPage_When_Searching_Then_ReturnsThatSliceAndFullTotalCount()
    {
        await SeedTutorAsync("Tutor 10", 10m);
        await SeedTutorAsync("Tutor 20", 20m);
        await SeedTutorAsync("Tutor 30", 30m);
        await SeedTutorAsync("Tutor 40", 40m);
        await SeedTutorAsync("Tutor 50", 50m);

        var result = await _handler.HandleAsync(new SearchTutorsQuery(Page: 2, PageSize: 2));

        result.Value.Tutors.Select(t => t.Name).Should().Equal("Tutor 30", "Tutor 40");
        result.Value.TotalCount.Should().Be(5);
        result.Value.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task Given_LastPartialPage_When_Searching_Then_ReturnsRemainingTutors()
    {
        await SeedTutorAsync("Tutor 10", 10m);
        await SeedTutorAsync("Tutor 20", 20m);
        await SeedTutorAsync("Tutor 30", 30m);

        var result = await _handler.HandleAsync(new SearchTutorsQuery(Page: 2, PageSize: 2));

        result.Value.Tutors.Select(t => t.Name).Should().Equal("Tutor 30");
    }

    [Fact]
    public async Task Given_PageBeyondResults_When_Searching_Then_ReturnsEmptyListWithTotalCount()
    {
        await SeedTutorAsync("Tutor 10", 10m);

        var result = await _handler.HandleAsync(new SearchTutorsQuery(Page: 5, PageSize: 2));

        result.Value.Tutors.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Given_FilterBySubject_When_Paging_Then_TotalCountReflectsFilteredTutorsOnly()
    {
        await SeedTutorAsync("Math 10", 10m, subjects: ["Matematica"]);
        await SeedTutorAsync("Math 20", 20m, subjects: ["Matematica"]);
        await SeedTutorAsync("Math 30", 30m, subjects: ["Matematica"]);
        await SeedTutorAsync("Info 15", 15m, subjects: ["Informatica"]);

        var result = await _handler.HandleAsync(new SearchTutorsQuery(Subject: "Matematica", Page: 1, PageSize: 2));

        result.Value.Tutors.Select(t => t.Name).Should().Equal("Math 10", "Math 20");
        result.Value.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Given_TutorWithSeveralSubjects_When_Searching_Then_ReturnsAllSubjectNamesInAlphabeticalOrder()
    {
        await SeedTutorAsync("Multi Subject", 50m, subjects: ["Matematica", "Algoritmi", "Fizica"]);

        var result = await _handler.HandleAsync(new SearchTutorsQuery());

        result.Value.Tutors.Single().Subjects.Should().Equal("Algoritmi", "Fizica", "Matematica");
    }

    [Fact]
    public async Task Given_TutorWithoutSubjects_When_Searching_Then_ReturnsEmptySubjectList()
    {
        await SeedTutorAsync("No Subjects", 50m);

        var result = await _handler.HandleAsync(new SearchTutorsQuery());

        result.Value.Tutors.Single().Subjects.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task Given_SubjectFilter_When_Searching_Then_ReturnsAllSubjectsOfMatchingTutor()
    {
        await SeedTutorAsync("Math And Physics", 50m, subjects: ["Matematica", "Fizica"]);

        var result = await _handler.HandleAsync(new SearchTutorsQuery(Subject: "Mate"));

        result.Value.Tutors.Single().Subjects.Should().Equal("Fizica", "Matematica");
    }

    [Fact]
    public async Task Given_SeveralTutors_When_Searching_Then_EachTutorGetsOnlyItsOwnSubjects()
    {
        await SeedTutorAsync("Math", 10m, subjects: ["Matematica"]);
        await SeedTutorAsync("Info", 20m, subjects: ["Informatica", "SQL"]);
        await SeedTutorAsync("Nothing", 30m);

        var result = await _handler.HandleAsync(new SearchTutorsQuery());

        var byName = result.Value.Tutors.ToDictionary(t => t.Name);
        byName["Math"].Subjects.Should().Equal("Matematica");
        byName["Info"].Subjects.Should().Equal("Informatica", "SQL");
        byName["Nothing"].Subjects.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_SecondPage_When_Searching_Then_ReturnsSubjectsOfTutorsOnThatPage()
    {
        await SeedTutorAsync("First", 10m, subjects: ["Matematica"]);
        await SeedTutorAsync("Second", 20m, subjects: ["Informatica"]);

        var result = await _handler.HandleAsync(new SearchTutorsQuery(Page: 2, PageSize: 1));

        result.Value.Tutors.Single().Subjects.Should().Equal("Informatica");
    }

    [Fact]
    public async Task Given_TutorWithRatings_When_Searching_Then_ReturnsAverageScoreAndRatingCount()
    {
        var tutor = await SeedTutorAsync("Rated Tutor", 70m);
        await SeedRatingsAsync(tutor, 5, 4);

        var result = await _handler.HandleAsync(new SearchTutorsQuery());

        var item = result.Value.Tutors.Single();
        item.AverageRating.Should().Be(4.5);
        item.RatingCount.Should().Be(2);
    }

    [Fact]
    public async Task Given_RatingsThatDoNotAverageToWholeNumber_When_Searching_Then_ReturnsExactAverage()
    {
        var tutor = await SeedTutorAsync("Rated Tutor", 70m);
        await SeedRatingsAsync(tutor, 5, 4, 4);

        var result = await _handler.HandleAsync(new SearchTutorsQuery());

        result.Value.Tutors.Single().AverageRating.Should().BeApproximately(13d / 3d, 1e-9);
        result.Value.Tutors.Single().RatingCount.Should().Be(3);
    }

    [Fact]
    public async Task Given_TutorWithoutRatings_When_Searching_Then_ReturnsZeroAverageAndZeroCount()
    {
        await SeedTutorAsync("Unrated Tutor", 70m);

        var result = await _handler.HandleAsync(new SearchTutorsQuery());

        var item = result.Value.Tutors.Single();
        item.AverageRating.Should().Be(0);
        item.RatingCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_SeveralTutorsWithDifferentRatings_When_Searching_Then_AggregatesEachTutorSeparately()
    {
        var excellent = await SeedTutorAsync("Excellent", 10m);
        var average = await SeedTutorAsync("Average", 20m);
        await SeedTutorAsync("Unrated", 30m);
        await SeedRatingsAsync(excellent, 5, 5, 5);
        await SeedRatingsAsync(average, 3, 2);

        var result = await _handler.HandleAsync(new SearchTutorsQuery());

        var byName = result.Value.Tutors.ToDictionary(t => t.Name);
        byName["Excellent"].AverageRating.Should().Be(5);
        byName["Excellent"].RatingCount.Should().Be(3);
        byName["Average"].AverageRating.Should().Be(2.5);
        byName["Average"].RatingCount.Should().Be(2);
        byName["Unrated"].AverageRating.Should().Be(0);
        byName["Unrated"].RatingCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_RatedTutorsOnSecondPage_When_Searching_Then_ReturnsAggregatesForThatPage()
    {
        await SeedTutorAsync("First", 10m);
        var second = await SeedTutorAsync("Second", 20m);
        await SeedRatingsAsync(second, 4);

        var result = await _handler.HandleAsync(new SearchTutorsQuery(Page: 2, PageSize: 1));

        var item = result.Value.Tutors.Single();
        item.Name.Should().Be("Second");
        item.AverageRating.Should().Be(4);
        item.RatingCount.Should().Be(1);
        result.Value.TotalCount.Should().Be(2);
    }
}
