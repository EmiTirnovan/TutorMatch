using System.Reflection;
using FluentAssertions;
using TutorMatch.Api.Domain.Tutors;
using Xunit;

namespace TutorMatch.Tests.Domain.Tutors;

public class TutorSubjectTests
{
    [Fact]
    public void Given_validIds_When_constructingTutorSubject_Then_initializesProperties()
    {
        var tutorId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();

        var link = new TutorSubject(tutorId, subjectId);

        link.TutorId.Should().Be(tutorId);
        link.SubjectId.Should().Be(subjectId);
    }

    [Fact]
    public void Given_emptyTutorId_When_constructingTutorSubject_Then_throwsArgumentException()
    {
        var action = () => new TutorSubject(Guid.Empty, Guid.NewGuid());

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Given_emptySubjectId_When_constructingTutorSubject_Then_throwsArgumentException()
    {
        var action = () => new TutorSubject(Guid.NewGuid(), Guid.Empty);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Given_tutorAndSubject_When_constructingTutorSubject_Then_initializesNavigations()
    {
        var tutor = new Tutor("Emi Tirnovan", "emi@example.com", "Bio", 100m);
        var subject = new Subject("Algoritmi");

        var link = new TutorSubject(tutor, subject);

        link.TutorId.Should().Be(tutor.Id);
        link.SubjectId.Should().Be(subject.Id);
        link.Tutor.Should().BeSameAs(tutor);
        link.Subject.Should().BeSameAs(subject);
    }

    [Fact]
    public void Given_nullTutor_When_constructingTutorSubject_Then_throwsArgumentNullException()
    {
        var action = () => new TutorSubject((Tutor)null!, new Subject("Algoritmi"));

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Given_nullSubject_When_constructingTutorSubject_Then_throwsArgumentNullException()
    {
        var action = () => new TutorSubject(new Tutor("Emi", "emi@test.com", "Bio", 80m), (Subject)null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Given_emptyTutorId_When_constructingTutorSubjectWithEntity_Then_throwsArgumentException()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m, Guid.Empty);
        var subject = new Subject("Algoritmi");

        var action = () => new TutorSubject(tutor, subject);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Given_emptySubjectId_When_constructingTutorSubjectWithEntity_Then_throwsArgumentException()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);
        var subject = new Subject("Algoritmi", id: Guid.Empty);

        var action = () => new TutorSubject(tutor, subject);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Given_privateConstructor_When_invokingViaReflection_Then_createsInstance()
    {
        var ctor = typeof(TutorSubject).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, binder: null, Type.EmptyTypes, modifiers: null)!;

        var instance = ctor.Invoke(null);

        instance.Should().NotBeNull();
        ((TutorSubject)instance!).TutorId.Should().Be(Guid.Empty);
    }
}

