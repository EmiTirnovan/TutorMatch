namespace TutorMatch.Api.Domain.Tutors;

/// <summary>
/// Reprezinta vectorul binar de embedding asociat profilului unui tutore pentru cautarea semantica.
/// </summary>
public class TutorEmbedding
{
    public Guid TutorId { get; private set; }
    public Tutor Tutor { get; private set; } = null!;
    public byte[] Embedding { get; private set; } = Array.Empty<byte>();
    public DateTime UpdatedAt { get; private set; }

    private TutorEmbedding() { } // Necesar pentru EF Core

    public TutorEmbedding(Guid tutorId, byte[] embedding)
    {
        if (tutorId == Guid.Empty)
        {
            throw new ArgumentException("TutorId cannot be empty.", nameof(tutorId));
        }

        ArgumentNullException.ThrowIfNull(embedding);

        TutorId = tutorId;
        Embedding = embedding;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateEmbedding(byte[] embedding)
    {
        ArgumentNullException.ThrowIfNull(embedding);
        Embedding = embedding;
        UpdatedAt = DateTime.UtcNow;
    }
}

