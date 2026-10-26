using FluentAssertions;
using TutorMatch.Api.Domain.Common;
using Xunit;

namespace TutorMatch.Tests.Domain.Common;

public class DateRangeTests
{
    [Fact]
    public void Given_validDates_When_constructingDateRange_Then_initializesValues()
    {
        var start = new DateTime(2026, 10, 20, 10, 0, 0);
        var end = new DateTime(2026, 10, 20, 12, 0, 0);

        var range = new DateRange(start, end);

        range.Start.Should().Be(start);
        range.End.Should().Be(end);
        range.Duration.Should().Be(TimeSpan.FromHours(2));
    }

    [Fact]
    public void Given_equalStartAndEnd_When_constructingDateRange_Then_throwsArgumentException()
    {
        var sameDate = new DateTime(2026, 10, 20, 10, 0, 0);

        var action = () => new DateRange(sameDate, sameDate);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Given_startAfterEnd_When_constructingDateRange_Then_throwsArgumentException()
    {
        var start = new DateTime(2026, 10, 20, 12, 0, 0);
        var end = new DateTime(2026, 10, 20, 10, 0, 0);

        var action = () => new DateRange(start, end);

        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("2026-10-20 10:00", "2026-10-20 11:00", "2026-10-20 11:00", "2026-10-20 12:00", false)]
    [InlineData("2026-10-20 11:00", "2026-10-20 12:00", "2026-10-20 10:00", "2026-10-20 11:00", false)]
    [InlineData("2026-10-20 08:00", "2026-10-20 09:00", "2026-10-20 10:00", "2026-10-20 11:00", false)]
    [InlineData("2026-10-20 12:00", "2026-10-20 13:00", "2026-10-20 10:00", "2026-10-20 11:00", false)]
    [InlineData("2026-10-20 10:00", "2026-10-20 11:00", "2026-10-20 10:30", "2026-10-20 11:30", true)]
    [InlineData("2026-10-20 10:00", "2026-10-20 11:00", "2026-10-20 09:30", "2026-10-20 10:30", true)]
    [InlineData("2026-10-20 10:00", "2026-10-20 12:00", "2026-10-20 10:30", "2026-10-20 11:30", true)]
    [InlineData("2026-10-20 10:30", "2026-10-20 11:30", "2026-10-20 10:00", "2026-10-20 12:00", true)]
    [InlineData("2026-10-20 10:00", "2026-10-20 11:00", "2026-10-20 10:00", "2026-10-20 11:00", true)]
    public void Given_twoRanges_When_checkingOverlap_Then_returnsExpectedResult(string start1, string end1, string start2, string end2, bool expected)
    {
        var range1 = new DateRange(DateTime.Parse(start1), DateTime.Parse(end1));
        var range2 = new DateRange(DateTime.Parse(start2), DateTime.Parse(end2));

        var result1 = range1.Overlaps(range2);
        var result2 = range2.Overlaps(range1);

        result1.Should().Be(expected);
        result2.Should().Be(expected);
    }

    [Fact]
    public void Given_nullRange_When_checkingOverlap_Then_throwsArgumentNullException()
    {
        var range = new DateRange(DateTime.UtcNow, DateTime.UtcNow.AddHours(1));

        var action = () => range.Overlaps(null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Given_datetimeInsideRange_When_checkingContains_Then_returnsTrue()
    {
        var range = new DateRange(new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 12, 0, 0));

        range.Contains(new DateTime(2026, 10, 20, 10, 0, 0)).Should().BeTrue();
        range.Contains(new DateTime(2026, 10, 20, 11, 0, 0)).Should().BeTrue();
        range.Contains(new DateTime(2026, 10, 20, 12, 0, 0)).Should().BeFalse();
        range.Contains(new DateTime(2026, 10, 20, 9, 59, 59)).Should().BeFalse();
        range.Contains(new DateTime(2026, 10, 20, 12, 0, 1)).Should().BeFalse();
    }

    [Fact]
    public void Given_dateRangeContainedWithinOuterRange_When_checkingContains_Then_returnsExpectedResult()
    {
        var outerRange = new DateRange(new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 14, 0, 0));
        var subRange = new DateRange(new DateTime(2026, 10, 20, 11, 0, 0), new DateTime(2026, 10, 20, 12, 0, 0));
        var identicalRange = new DateRange(new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 14, 0, 0));
        var spillingBeforeRange = new DateRange(new DateTime(2026, 10, 20, 9, 0, 0), new DateTime(2026, 10, 20, 12, 0, 0));
        var spillingAfterRange = new DateRange(new DateTime(2026, 10, 20, 12, 0, 0), new DateTime(2026, 10, 20, 15, 0, 0));

        outerRange.Contains(subRange).Should().BeTrue();
        outerRange.Contains(identicalRange).Should().BeTrue();
        outerRange.Contains(spillingBeforeRange).Should().BeFalse();
        outerRange.Contains(spillingAfterRange).Should().BeFalse();
    }

    [Fact]
    public void Given_equalRanges_When_checkingEquality_Then_valuesAreEqual()
    {
        var firstRange = new DateRange(new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 12, 0, 0));
        var secondRange = new DateRange(new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 12, 0, 0));
        var differentRange = new DateTime(2026, 10, 20, 10, 0, 0);

        firstRange.Should().Be(secondRange);
        firstRange.Should().NotBeSameAs(secondRange);
        firstRange.Equals(differentRange).Should().BeFalse();
    }
}
