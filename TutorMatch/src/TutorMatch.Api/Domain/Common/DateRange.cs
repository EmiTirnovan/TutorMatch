namespace TutorMatch.Api.Domain.Common;

public sealed record DateRange
{
    public DateTime Start { get; }
    public DateTime End { get; }

    public DateRange(DateTime start, DateTime end)
    {
        if (start == default)
        {
            throw new ArgumentException("Start date cannot be default.", nameof(start));
        }

        if (end == default)
        {
            throw new ArgumentException("End date cannot be default.", nameof(end));
        }

        if (end <= start)
        {
            throw new ArgumentException("End date must be strictly after start date.", nameof(end));
        }

        Start = start;
        End = end;
    }

    public TimeSpan Duration => End - Start;

    public bool Overlaps(DateRange other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Start < other.End && End > other.Start;
    }

    public bool Contains(DateTime dateTime)
    {
        return dateTime >= Start && dateTime <= End;
    }
}
