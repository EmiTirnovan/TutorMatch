namespace TutorMatch.Tests.Domain.Tutors;

using System.Collections.ObjectModel;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Tutors;

public class TutorTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateTutor()
    {
        var tutor = new Tutor("Emi Tirnovan", "emi@example.com", "Student anul 3", 80m);

        Assert.NotEqual(Guid.Empty, tutor.Id);
        Assert.Equal("Emi Tirnovan", tutor.Name);
        Assert.Equal("emi@example.com", tutor.Email);
        Assert.Equal("Student anul 3", tutor.Bio);
        Assert.Equal(80m, tutor.HourlyRate);
        Assert.Empty(tutor.Availability);
        Assert.Empty(tutor.TutorSubjects);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ShouldThrowArgumentException(string name)
    {
        Assert.Throws<ArgumentException>(() => new Tutor(name, "test@test.com", "Bio", 50m));
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData("test.com")]
    public void Constructor_WithInvalidEmail_ShouldThrowArgumentException(string email)
    {
        Assert.Throws<ArgumentException>(() => new Tutor("Name", email, "Bio", 50m));
    }

    [Fact]
    public void Constructor_WithNullBio_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Tutor("Name", "test@test.com", null!, 50m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Constructor_WithInvalidHourlyRate_ShouldThrowArgumentException(decimal rate)
    {
        Assert.Throws<ArgumentException>(() => new Tutor("Name", "test@test.com", "Bio", rate));
    }

    [Fact]
    public void Collections_ShouldBeEncapsulatedAndReadOnly()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);

        Assert.IsAssignableFrom<IReadOnlyList<DateRange>>(tutor.Availability);
        Assert.IsAssignableFrom<IReadOnlyList<TutorSubject>>(tutor.TutorSubjects);

        // Verificam ca interfata IList expusa intern este read-only
        var availabilityCollection = (IList<DateRange>)tutor.Availability;
        Assert.True(availabilityCollection.IsReadOnly);

        var subjectsCollection = (IList<TutorSubject>)tutor.TutorSubjects;
        Assert.True(subjectsCollection.IsReadOnly);
    }

    [Fact]
    public void AddAvailability_WithValidRange_ShouldAddSuccessfully()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);
        var slot = new DateRange(new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 12, 0, 0));

        tutor.AddAvailability(slot);

        Assert.Single(tutor.Availability);
        Assert.Equal(slot, tutor.Availability[0]);
    }

    [Fact]
    public void AddAvailability_WithOverlappingRange_ShouldThrowInvalidOperationException()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);
        var slot1 = new DateRange(new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 12, 0, 0));
        var slot2 = new DateRange(new DateTime(2026, 10, 20, 11, 0, 0), new DateTime(2026, 10, 20, 13, 0, 0)); // se suprapune

        tutor.AddAvailability(slot1);

        var ex = Assert.Throws<InvalidOperationException>(() => tutor.AddAvailability(slot2));
        Assert.Contains("se suprapune", ex.Message);
        Assert.Single(tutor.Availability);
    }

    [Fact]
    public void AddAvailability_WithAdjacentRange_ShouldSucceed()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);
        var slot1 = new DateRange(new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 11, 0, 0));
        var slot2 = new DateRange(new DateTime(2026, 10, 20, 11, 0, 0), new DateTime(2026, 10, 20, 12, 0, 0)); // adiacent

        tutor.AddAvailability(slot1);
        tutor.AddAvailability(slot2);

        Assert.Equal(2, tutor.Availability.Count);
    }

    [Fact]
    public void RemoveAvailability_ShouldRemoveSlot()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);
        var slot = new DateRange(new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 12, 0, 0));
        tutor.AddAvailability(slot);

        var removed = tutor.RemoveAvailability(slot);

        Assert.True(removed);
        Assert.Empty(tutor.Availability);
    }

    [Fact]
    public void UpdateAvailability_WithValidNonOverlappingList_ShouldReplaceAvailability()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);
        tutor.AddAvailability(new DateRange(new DateTime(2026, 10, 20, 9, 0, 0), new DateTime(2026, 10, 20, 10, 0, 0)));

        var newSlots = new List<DateRange>
        {
            new(new DateTime(2026, 10, 21, 10, 0, 0), new DateTime(2026, 10, 21, 12, 0, 0)),
            new(new DateTime(2026, 10, 21, 14, 0, 0), new DateTime(2026, 10, 21, 16, 0, 0))
        };

        tutor.UpdateAvailability(newSlots);

        Assert.Equal(2, tutor.Availability.Count);
        Assert.Equal(newSlots[0], tutor.Availability[0]);
        Assert.Equal(newSlots[1], tutor.Availability[1]);
    }

    [Fact]
    public void UpdateAvailability_WithOverlappingList_ShouldThrowAndPreservePreviousState()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);
        var originalSlot = new DateRange(new DateTime(2026, 10, 20, 9, 0, 0), new DateTime(2026, 10, 20, 10, 0, 0));
        tutor.AddAvailability(originalSlot);

        var conflictingSlots = new List<DateRange>
        {
            new(new DateTime(2026, 10, 21, 10, 0, 0), new DateTime(2026, 10, 21, 12, 0, 0)),
            new(new DateTime(2026, 10, 21, 11, 30, 0), new DateTime(2026, 10, 21, 13, 0, 0)) // se suprapune
        };

        Assert.Throws<InvalidOperationException>(() => tutor.UpdateAvailability(conflictingSlots));
        Assert.Single(tutor.Availability);
        Assert.Equal(originalSlot, tutor.Availability[0]);
    }

    [Fact]
    public void IsAvailableDuring_ShouldCheckIfRangeIsContained()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);
        tutor.AddAvailability(new DateRange(new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 14, 0, 0)));

        var validBooking = new DateRange(new DateTime(2026, 10, 20, 11, 0, 0), new DateTime(2026, 10, 20, 12, 0, 0));
        var outOfRangeBooking = new DateRange(new DateTime(2026, 10, 20, 13, 0, 0), new DateTime(2026, 10, 20, 15, 0, 0));

        Assert.True(tutor.IsAvailableDuring(validBooking));
        Assert.False(tutor.IsAvailableDuring(outOfRangeBooking));
    }

    [Fact]
    public void AddSubject_ShouldAddTutorSubjectLink()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);
        var subject = new Subject("Algoritmi");

        tutor.AddSubject(subject);

        Assert.Single(tutor.TutorSubjects);
        Assert.Equal(subject.Id, tutor.TutorSubjects[0].SubjectId);
    }

    [Fact]
    public void AddSubject_DuplicateSubject_ShouldThrowInvalidOperationException()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);
        var subject = new Subject("Algoritmi");
        tutor.AddSubject(subject);

        var ex = Assert.Throws<InvalidOperationException>(() => tutor.AddSubject(subject));
        Assert.Contains("deja asociata", ex.Message);
    }

    [Fact]
    public void RemoveSubject_ShouldRemoveLink()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 60m);
        var subject = new Subject("Algoritmi");
        tutor.AddSubject(subject);

        var removed = tutor.RemoveSubject(subject.Id);

        Assert.True(removed);
        Assert.Empty(tutor.TutorSubjects);
    }

    [Fact]
    public void UpdateProfile_WithValidData_ShouldUpdate()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio vechi", 50m);

        tutor.UpdateProfile("Emi Tirnovan", "Bio nou", 75m);

        Assert.Equal("Emi Tirnovan", tutor.Name);
        Assert.Equal("Bio nou", tutor.Bio);
        Assert.Equal(75m, tutor.HourlyRate);
    }
}
