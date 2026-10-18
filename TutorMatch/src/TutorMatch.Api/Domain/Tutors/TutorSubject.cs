namespace TutorMatch.Api.Domain.Tutors;

// Tabela de legatura pentru relatia Many-to-Many dintre Tutor si Subject
public class TutorSubject
{
    public Guid TutorId { get; private set; }
    public Tutor Tutor { get; private set; } = null!;

    public Guid SubjectId { get; private set; }
    public Subject Subject { get; private set; } = null!;

    private TutorSubject() { } // pt EF Core

    public TutorSubject(Guid tutorId, Guid subjectId)
    {
        if (tutorId == Guid.Empty)
        {
            throw new ArgumentException("TutorId nu poate fi gol.", nameof(tutorId));
        }

        if (subjectId == Guid.Empty)
        {
            throw new ArgumentException("SubjectId nu poate fi gol.", nameof(subjectId));
        }

        TutorId = tutorId;
        SubjectId = subjectId;
    }

    public TutorSubject(Tutor tutor, Subject subject)
    {
        ArgumentNullException.ThrowIfNull(tutor);
        ArgumentNullException.ThrowIfNull(subject);

        if (tutor.Id == Guid.Empty)
        {
            throw new ArgumentException("TutorId nu poate fi gol.", nameof(tutor));
        }

        if (subject.Id == Guid.Empty)
        {
            throw new ArgumentException("SubjectId nu poate fi gol.", nameof(subject));
        }

        Tutor = tutor;
        TutorId = tutor.Id;
        Subject = subject;
        SubjectId = subject.Id;
    }
}
