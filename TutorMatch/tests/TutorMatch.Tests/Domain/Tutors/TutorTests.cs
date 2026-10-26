using System.Reflection;
using FluentAssertions;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Tutors;
using Xunit;

namespace TutorMatch.Tests.Domain.Tutors;

public class TutorTests
{
    [Fact]
    public void Given_validData_When_constructingTutor_Then_initializesValues()
    {
        var tutor = new Tutor("Emi Tirnovan", "emi@example.com", "Student anul 3", 80m);

        tutor.Id.Should().NotBeEmpty();
        tutor.Name.Should().Be("Emi Tirnovan");
        tutor.Email.Should().Be("emi@example.com");
        tutor.Bio.Should().Be("Student anul 3");
        tutor.HourlyRate.Should().Be(80m);
        tutor.Availability.Should().BeEmpty();
        tutor.TutorSubjects.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Given_invalidName_When_constructingTutor_Then_throwsArgumentException(string name)
    {
        var action = () => new Tutor(name, "test@test.com", "Bio", 50m);

        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData("test.com")]
    public void Given_invalidEmail_When_constructingTutor_Then_throwsArgumentException(string email)
    {
        var action = () => new Tutor("Name", email, "Bio", 50m);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Given_nullBio_When_constructingTutor_Then_throwsArgumentNullException()
    {
        var action = () => new Tutor("Name", "test@test.com", null!, 50m);

        action.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Given_invalidHourlyRate_When_constructingTutor_Then_throwsArgumentException(decimal rate)
    {
        var action = () => new Tutor("Name", "test@test.com", "Bio", rate);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Given_validRange_When_addingAvailability_Then_addsToCollection()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);
        var slot = new DateRange(new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 12, 0, 0));

        tutor.AddAvailability(slot);

        tutor.Availability.Should().ContainSingle().Which.Should().Be(slot);
    }

    [Fact]
    public void Given_overlappingRange_When_addingAvailability_Then_throwsInvalidOperationException()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);
        var existingSlot = new DateRange(new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 12, 0, 0));
        var overlappingSlot = new DateRange(new DateTime(2026, 10, 20, 11, 0, 0), new DateTime(2026, 10, 20, 13, 0, 0));
        tutor.AddAvailability(existingSlot);

        var action = () => tutor.AddAvailability(overlappingSlot);

        action.Should().Throw<InvalidOperationException>();
        tutor.Availability.Should().ContainSingle().Which.Should().Be(existingSlot);
    }

    [Fact]
    public void Given_validRange_When_removingAvailability_Then_removesSlot()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);
        var slot = new DateRange(new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 12, 0, 0));
        tutor.AddAvailability(slot);

        var removed = tutor.RemoveAvailability(slot);

        removed.Should().BeTrue();
        tutor.Availability.Should().BeEmpty();
    }

    [Fact]
    public void Given_validSlots_When_updatingAvailability_Then_replacesCollection()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);
        tutor.AddAvailability(new DateRange(new DateTime(2026, 10, 20, 9, 0, 0), new DateTime(2026, 10, 20, 10, 0, 0)));
        var newSlots = new List<DateRange>
        {
            new(new DateTime(2026, 10, 21, 10, 0, 0), new DateTime(2026, 10, 21, 12, 0, 0)),
            new(new DateTime(2026, 10, 21, 14, 0, 0), new DateTime(2026, 10, 21, 16, 0, 0))
        };

        tutor.UpdateAvailability(newSlots);

        tutor.Availability.Should().HaveCount(2);
        tutor.Availability.Should().ContainInOrder(newSlots);
    }

    [Fact]
    public void Given_conflictingSlots_When_updatingAvailability_Then_throwsAndPreservesPreviousState()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);
        var originalSlot = new DateRange(new DateTime(2026, 10, 20, 9, 0, 0), new DateTime(2026, 10, 20, 10, 0, 0));
        tutor.AddAvailability(originalSlot);
        var conflictingSlots = new List<DateRange>
        {
            new(new DateTime(2026, 10, 21, 10, 0, 0), new DateTime(2026, 10, 21, 12, 0, 0)),
            new(new DateTime(2026, 10, 21, 11, 30, 0), new DateTime(2026, 10, 21, 13, 0, 0))
        };

        var action = () => tutor.UpdateAvailability(conflictingSlots);

        action.Should().Throw<InvalidOperationException>();
        tutor.Availability.Should().ContainSingle().Which.Should().Be(originalSlot);
    }

    [Fact]
    public void Given_validBookingRange_When_checkingAvailability_Then_returnsExpectedResult()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);
        tutor.AddAvailability(new DateRange(new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 14, 0, 0)));
        var validBooking = new DateRange(new DateTime(2026, 10, 20, 11, 0, 0), new DateTime(2026, 10, 20, 12, 0, 0));
        var outOfRangeBooking = new DateRange(new DateTime(2026, 10, 20, 13, 0, 0), new DateTime(2026, 10, 20, 15, 0, 0));

        tutor.IsAvailableDuring(validBooking).Should().BeTrue();
        tutor.IsAvailableDuring(outOfRangeBooking).Should().BeFalse();
    }

    [Fact]
    public void Given_validSubject_When_addingSubject_Then_addsLink()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);
        var subject = new Subject("Algoritmi");

        tutor.AddSubject(subject);

        tutor.TutorSubjects.Should().ContainSingle();
        tutor.TutorSubjects[0].SubjectId.Should().Be(subject.Id);
    }

    [Fact]
    public void Given_duplicateSubject_When_addingSubject_Then_throwsInvalidOperationException()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);
        var subject = new Subject("Algoritmi");
        tutor.AddSubject(subject);

        var action = () => tutor.AddSubject(subject);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Given_validProfile_When_updatingProfile_Then_updatesValues()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio vechi", 50m);

        tutor.UpdateProfile("Emi Tirnovan", "Bio nou", 75m);

        tutor.Name.Should().Be("Emi Tirnovan");
        tutor.Bio.Should().Be("Bio nou");
        tutor.HourlyRate.Should().Be(75m);
    }

    [Fact]
    public void Given_nullRange_When_addingAvailability_Then_throwsArgumentNullException()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);

        var action = () => tutor.AddAvailability(null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Given_missingRange_When_removingAvailability_Then_returnsFalse()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);
        var range = new DateRange(new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 12, 0, 0));

        var removed = tutor.RemoveAvailability(range);

        removed.Should().BeFalse();
    }

    [Fact]
    public void Given_nullAvailability_When_updatingAvailability_Then_throwsArgumentNullException()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);

        var action = () => tutor.UpdateAvailability(null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Given_nullRange_When_checkingAvailability_Then_throwsArgumentNullException()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);

        var action = () => tutor.IsAvailableDuring(null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Given_nullSubject_When_addingSubject_Then_throwsArgumentNullException()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);

        var action = () => tutor.AddSubject((Subject)null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Given_emptySubjectId_When_addingSubjectById_Then_throwsArgumentException()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);

        var action = () => tutor.AddSubject(Guid.Empty);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Given_existingSubject_When_removingSubject_Then_returnsTrue()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);
        var subject = new Subject("Algoritmi");
        tutor.AddSubject(subject);

        var removed = tutor.RemoveSubject(subject.Id);

        removed.Should().BeTrue();
        tutor.TutorSubjects.Should().BeEmpty();
    }

    [Fact]
    public void Given_missingSubject_When_removingSubject_Then_returnsFalse()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);

        var removed = tutor.RemoveSubject(Guid.NewGuid());

        removed.Should().BeFalse();
    }

    [Fact]
    public void Given_emptyName_When_updatingProfile_Then_throwsArgumentException()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);

        var action = () => tutor.UpdateProfile("   ", "Bio", 70m);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Given_existingSubjectId_When_addingSubjectById_Then_throwsInvalidOperationException()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);
        var subjectId = Guid.NewGuid();
        tutor.AddSubject(subjectId);

        var action = () => tutor.AddSubject(subjectId);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Given_privateConstructor_When_invokingViaReflection_Then_createsInstance()
    {
        var ctor = typeof(Tutor).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, binder: null, Type.EmptyTypes, modifiers: null)!;

        var instance = ctor.Invoke(null);

        instance.Should().NotBeNull();
        ((Tutor)instance!).Availability.Should().BeEmpty();
    }
}

