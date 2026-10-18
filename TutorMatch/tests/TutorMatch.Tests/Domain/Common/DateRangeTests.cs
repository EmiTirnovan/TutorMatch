namespace TutorMatch.Tests.Domain.Common;

using TutorMatch.Api.Domain.Common;

public class DateRangeTests
{
    [Fact]
    public void Constructor_WithValidDates_ShouldCreateInstance()
    {
        var start = new DateTime(2026, 10, 20, 10, 0, 0);
        var end = new DateTime(2026, 10, 20, 12, 0, 0);

        var range = new DateRange(start, end);

        Assert.Equal(start, range.Start);
        Assert.Equal(end, range.End);
        Assert.Equal(TimeSpan.FromHours(2), range.Duration);
    }

    [Fact]
    public void Constructor_WhenStartEqualsEnd_ShouldThrowArgumentException()
    {
        var sameDate = new DateTime(2026, 10, 20, 10, 0, 0);

        var ex = Assert.Throws<ArgumentException>(() => new DateRange(sameDate, sameDate));
        Assert.Contains("Data de inceput trebuie sa fie inainte", ex.Message);
    }

    [Fact]
    public void Constructor_WhenStartIsAfterEnd_ShouldThrowArgumentException()
    {
        var start = new DateTime(2026, 10, 20, 12, 0, 0);
        var end = new DateTime(2026, 10, 20, 10, 0, 0);

        var ex = Assert.Throws<ArgumentException>(() => new DateRange(start, end));
        Assert.Contains("Data de inceput trebuie sa fie inainte", ex.Message);
    }

    [Theory]
    // Intervale adiacente (nu se suprapun)
    [InlineData("2026-10-20 10:00", "2026-10-20 11:00", "2026-10-20 11:00", "2026-10-20 12:00", false)]
    [InlineData("2026-10-20 11:00", "2026-10-20 12:00", "2026-10-20 10:00", "2026-10-20 11:00", false)]
    // Intervale separate
    [InlineData("2026-10-20 08:00", "2026-10-20 09:00", "2026-10-20 10:00", "2026-10-20 11:00", false)]
    [InlineData("2026-10-20 12:00", "2026-10-20 13:00", "2026-10-20 10:00", "2026-10-20 11:00", false)]
    // Suprapuneri partiale
    [InlineData("2026-10-20 10:00", "2026-10-20 11:00", "2026-10-20 10:30", "2026-10-20 11:30", true)]
    [InlineData("2026-10-20 10:00", "2026-10-20 11:00", "2026-10-20 09:30", "2026-10-20 10:30", true)]
    // Interval inclus
    [InlineData("2026-10-20 10:00", "2026-10-20 12:00", "2026-10-20 10:30", "2026-10-20 11:30", true)]
    // Interval mai mare
    [InlineData("2026-10-20 10:30", "2026-10-20 11:30", "2026-10-20 10:00", "2026-10-20 12:00", true)]
    // Identice
    [InlineData("2026-10-20 10:00", "2026-10-20 11:00", "2026-10-20 10:00", "2026-10-20 11:00", true)]
    public void Overlaps_ShouldReturnExpectedResult(
        string start1, string end1,
        string start2, string end2,
        bool expected)
    {
        var range1 = new DateRange(DateTime.Parse(start1), DateTime.Parse(end1));
        var range2 = new DateRange(DateTime.Parse(start2), DateTime.Parse(end2));

        var result1 = range1.Overlaps(range2);
        var result2 = range2.Overlaps(range1);

        Assert.Equal(expected, result1);
        Assert.Equal(expected, result2);
    }

    [Fact]
    public void Overlaps_WithNull_ShouldThrowArgumentNullException()
    {
        var range = new DateRange(DateTime.UtcNow, DateTime.UtcNow.AddHours(1));
        Assert.Throws<ArgumentNullException>(() => range.Overlaps(null!));
    }

    [Fact]
    public void Contains_DateTime_ShouldReturnTrue_WhenInsideInterval()
    {
        var range = new DateRange(new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 12, 0, 0));

        Assert.True(range.Contains(new DateTime(2026, 10, 20, 10, 0, 0))); // start inclusiv
        Assert.True(range.Contains(new DateTime(2026, 10, 20, 11, 0, 0))); // interior
        Assert.False(range.Contains(new DateTime(2026, 10, 20, 12, 0, 0))); // end exclusiv
        Assert.False(range.Contains(new DateTime(2026, 10, 20, 9, 59, 59)));
        Assert.False(range.Contains(new DateTime(2026, 10, 20, 12, 0, 1)));
    }

    [Fact]
    public void Contains_DateRange_ShouldReturnExpectedResult()
    {
        var outer = new DateRange(new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 14, 0, 0));
        var subRange = new DateRange(new DateTime(2026, 10, 20, 11, 0, 0), new DateTime(2026, 10, 20, 12, 0, 0));
        var identical = new DateRange(new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 14, 0, 0));
        var spillingBefore = new DateRange(new DateTime(2026, 10, 20, 9, 0, 0), new DateTime(2026, 10, 20, 12, 0, 0));
        var spillingAfter = new DateRange(new DateTime(2026, 10, 20, 12, 0, 0), new DateTime(2026, 10, 20, 15, 0, 0));

        Assert.True(outer.Contains(subRange));
        Assert.True(outer.Contains(identical));
        Assert.False(outer.Contains(spillingBefore));
        Assert.False(outer.Contains(spillingAfter));
    }

    [Fact]
    public void Record_Equality_ShouldBeValueBased()
    {
        var r1 = new DateRange(new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 12, 0, 0));
        var r2 = new DateRange(new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 12, 0, 0));
        var r3 = new DateRange(new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 13, 0, 0));

        Assert.Equal(r1, r2);
        Assert.True(r1 == r2);
        Assert.NotEqual(r1, r3);
        Assert.True(r1 != r3);
        Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
    }
}
