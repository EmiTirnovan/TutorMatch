namespace TutorMatch.Api.Domain.Common;

// Value object imutabil pentru intervale de timp [Start, End)
public sealed record DateRange
{
    public DateTime Start { get; init; }
    public DateTime End { get; init; }

    // Constructor privat cerut de EF Core
    private DateRange() { }

    public DateRange(DateTime start, DateTime end)
    {
        if (start >= end)
        {
            throw new ArgumentException("Data de inceput trebuie sa fie inainte de data de sfarsit.", nameof(start));
        }

        Start = start;
        End = end;
    }

    public TimeSpan Duration => End - Start;

    // Doua intervale adiacente (ex: 10-11 si 11-12) nu se considera suprapuse
    public bool Overlaps(DateRange other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Start < other.End && End > other.Start;
    }

    // Verifica daca o data se afla in interiorul intervalului
    public bool Contains(DateTime dateTime)
    {
        return dateTime >= Start && dateTime < End;
    }

    // Verifica daca intervalul contine complet un alt interval
    public bool Contains(DateRange other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return other.Start >= Start && other.End <= End;
    }

    public override string ToString() => $"[{Start:yyyy-MM-dd HH:mm} - {End:yyyy-MM-dd HH:mm})";
}
