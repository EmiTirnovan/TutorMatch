namespace TutorMatch.Tests.Domain.Tutors;

using TutorMatch.Api.Domain.Tutors;

public class SubjectTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateSubject()
    {
        var subject = new Subject("Algoritmi si Structuri de Date", "Curs introductiv");

        Assert.NotEqual(Guid.Empty, subject.Id);
        Assert.Equal("Algoritmi si Structuri de Date", subject.Name);
        Assert.Equal("Curs introductiv", subject.Description);
        Assert.Empty(subject.TutorSubjects);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        Assert.Throws<ArgumentException>(() => new Subject(invalidName!));
    }

    [Fact]
    public void Update_ShouldUpdateNameAndDescription()
    {
        var subject = new Subject("POO", "Programare orientata pe obiecte");

        subject.Update("Programare Orientata pe Obiecte", "Actualizat");

        Assert.Equal("Programare Orientata pe Obiecte", subject.Name);
        Assert.Equal("Actualizat", subject.Description);
    }

    [Fact]
    public void Update_WithInvalidName_ShouldThrowArgumentException()
    {
        var subject = new Subject("POO");

        Assert.Throws<ArgumentException>(() => subject.Update("  ", null));
    }
}
