namespace TutorMatch.Tests.Domain.Tutors;

using TutorMatch.Api.Domain.Tutors;

public class TutorSubjectTests
{
    [Fact]
    public void Constructor_WithGuids_ShouldInitializeProperties()
    {
        var tutorId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();

        var link = new TutorSubject(tutorId, subjectId);

        Assert.Equal(tutorId, link.TutorId);
        Assert.Equal(subjectId, link.SubjectId);
    }

    [Fact]
    public void Constructor_WithEmptyTutorId_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new TutorSubject(Guid.Empty, Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_WithEmptySubjectId_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new TutorSubject(Guid.NewGuid(), Guid.Empty));
    }

    [Fact]
    public void Constructor_WithEntities_ShouldInitializeNavigationsAndIds()
    {
        var tutor = new Tutor("Emi Tirnovan", "emi@example.com", "Bio", 100m);
        var subject = new Subject("Algoritmi");

        var link = new TutorSubject(tutor, subject);

        Assert.Equal(tutor.Id, link.TutorId);
        Assert.Equal(subject.Id, link.SubjectId);
        Assert.Same(tutor, link.Tutor);
        Assert.Same(subject, link.Subject);
    }
}
