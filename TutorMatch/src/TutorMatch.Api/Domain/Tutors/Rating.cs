namespace TutorMatch.Api.Domain.Tutors;

public class Rating
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid TutorId { get; private set; }
    public Guid StudentId { get; private set; }
    public int Score { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Rating() { } 

    public Rating(Guid sessionId, Guid tutorId, Guid studentId, int score, string? notes = null)
    {
        if (score < 1 || score > 5)
            throw new ArgumentException("Scorul trebuie să fie intre 1 și 5.", nameof(score));

        if (sessionId == Guid.Empty || tutorId == Guid.Empty || studentId == Guid.Empty)
            throw new ArgumentException("ID-urile pentru sesiune, tutore si student sunt obligatorii.");

        Id = Guid.NewGuid();
        SessionId = sessionId;
        TutorId = tutorId;
        StudentId = studentId;
        Score = score;
        Notes = notes?.Trim();
        CreatedAt = DateTime.UtcNow;
    }
}