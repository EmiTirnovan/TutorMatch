using System.Reflection;
using FluentAssertions;
using TutorMatch.Api.Domain.Tutors;
using Xunit;

namespace TutorMatch.Tests.Domain.Tutors;

public class SubjectTests
{
    [Fact]
    public void Given_validData_When_constructingSubject_Then_initializesValues()
    {
        var subject = new Subject("Algoritmi si Structuri de Date", "Curs introductiv");

        subject.Id.Should().NotBeEmpty();
        subject.Name.Should().Be("Algoritmi si Structuri de Date");
        subject.Description.Should().Be("Curs introductiv");
        subject.UsageCount.Should().Be(0);
        subject.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        subject.TutorSubjects.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Given_invalidName_When_constructingSubject_Then_throwsArgumentException(string invalidName)
    {
        var action = () => new Subject(invalidName);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Given_whitespaceAroundAndInside_When_normalizingName_Then_trimsAndCollapsesSpaces()
    {
        var subject = new Subject("   Machine    Learning   ");

        subject.Name.Should().Be("Machine Learning");
    }

    [Fact]
    public void Given_usageCountOperations_When_callingMethods_Then_updatesUsageCountCorrectly()
    {
        var subject = new Subject("Java");

        subject.UsageCount.Should().Be(0);

        subject.IncrementUsage();
        subject.UsageCount.Should().Be(1);

        subject.IncrementUsage(3);
        subject.UsageCount.Should().Be(4);

        subject.DecrementUsage(2);
        subject.UsageCount.Should().Be(2);

        // Clamps at zero
        subject.DecrementUsage(10);
        subject.UsageCount.Should().Be(0);

        subject.SetUsageCount(5);
        subject.UsageCount.Should().Be(5);

        subject.SetUsageCount(-3);
        subject.UsageCount.Should().Be(0);
    }

    [Fact]
    public void Given_validName_When_updatingSubject_Then_updatesProperties()
    {
        var subject = new Subject("POO", "Programare orientata pe obiecte");

        subject.Update("Programare Orientata pe Obiecte", "Actualizat");

        subject.Name.Should().Be("Programare Orientata pe Obiecte");
        subject.Description.Should().Be("Actualizat");
    }

    [Fact]
    public void Given_nullDescription_When_constructingSubject_Then_setsDescriptionToNull()
    {
        var subject = new Subject("POO", null);

        subject.Description.Should().BeNull();
    }

    [Fact]
    public void Given_blankName_When_updatingSubject_Then_throwsArgumentException()
    {
        var subject = new Subject("POO");

        var action = () => subject.Update("  ", null);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Given_nullDescription_When_updatingSubject_Then_clearsDescription()
    {
        var subject = new Subject("POO", "Intro");

        subject.Update("Algoritmi", null);

        subject.Description.Should().BeNull();
    }

    [Fact]
    public void Given_privateConstructor_When_invokingViaReflection_Then_createsInstance()
    {
        var ctor = typeof(Subject).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, binder: null, Type.EmptyTypes, modifiers: null)!;

        var instance = ctor.Invoke(null);

        instance.Should().NotBeNull();
        ((Subject)instance!).TutorSubjects.Should().BeEmpty();
    }
}
