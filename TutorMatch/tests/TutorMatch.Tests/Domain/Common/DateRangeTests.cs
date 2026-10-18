using FluentAssertions;
using TutorMatch.Api.Domain.Common;
using Xunit;

namespace TutorMatch.Tests.Domain.Common;

public class DateRangeTests
{
    [Fact]
    public void Constructor_ValidStartAndEnd_ShouldCreateDateRange()
    {
        var start = DateTime.UtcNow;
        var end = start.AddHours(2);

        var dateRange = new DateRange(start, end);

        dateRange.Start.Should().Be(start);
        dateRange.End.Should().Be(end);
        dateRange.Duration.Should().Be(TimeSpan.FromHours(2));
    }

    [Fact]
    public void Constructor_EndBeforeOrEqualToStart_ShouldThrowArgumentException()
    {
        var now = DateTime.UtcNow;

        var actEqual = () => new DateRange(now, now);
        var actBefore = () => new DateRange(now, now.AddMinutes(-30));

        actEqual.Should().Throw<ArgumentException>();
        actBefore.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_DefaultDates_ShouldThrowArgumentException()
    {
        var valid = DateTime.UtcNow;

        var actStartDefault = () => new DateRange(default, valid);
        var actEndDefault = () => new DateRange(valid, default);

        actStartDefault.Should().Throw<ArgumentException>();
        actEndDefault.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Overlaps_OverlappingRanges_ShouldReturnTrue()
    {
        var baseStart = DateTime.UtcNow;
        var range1 = new DateRange(baseStart, baseStart.AddHours(2));
        var range2 = new DateRange(baseStart.AddHours(1), baseStart.AddHours(3));

        range1.Overlaps(range2).Should().BeTrue();
        range2.Overlaps(range1).Should().BeTrue();
    }

    [Fact]
    public void Overlaps_NonOverlappingRanges_ShouldReturnFalse()
    {
        var baseStart = DateTime.UtcNow;
        var range1 = new DateRange(baseStart, baseStart.AddHours(1));
        var range2 = new DateRange(baseStart.AddHours(1), baseStart.AddHours(2));

        range1.Overlaps(range2).Should().BeFalse();
        range2.Overlaps(range1).Should().BeFalse();
    }

    [Fact]
    public void Contains_DateInsideRange_ShouldReturnTrue()
    {
        var baseStart = DateTime.UtcNow;
        var range = new DateRange(baseStart, baseStart.AddHours(2));

        range.Contains(baseStart.AddHours(1)).Should().BeTrue();
        range.Contains(baseStart.AddHours(3)).Should().BeFalse();
    }
}
