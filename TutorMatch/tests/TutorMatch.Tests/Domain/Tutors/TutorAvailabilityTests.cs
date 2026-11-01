using FluentAssertions;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Domain.Tutors.Events;
using Xunit;

namespace TutorMatch.Tests.Domain.Tutors;

public class TutorAvailabilityTests
{
    [Fact]
    public void Given_ValidIntervals_When_UpdatingAvailability_Then_ReplacesExistingAvailability()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);
        tutor.AddAvailability(new DateRange(new DateTime(2026, 11, 1, 9, 0, 0), new DateTime(2026, 11, 1, 10, 0, 0)));
        var newSlots = new List<DateRange>
        {
            new(new DateTime(2026, 11, 2, 10, 0, 0), new DateTime(2026, 11, 2, 12, 0, 0)),
            new(new DateTime(2026, 11, 2, 14, 0, 0), new DateTime(2026, 11, 2, 16, 0, 0))
        };

        tutor.UpdateAvailability(newSlots);

        tutor.Availability.Should().HaveCount(2);
        tutor.Availability.Should().ContainInOrder(newSlots);
    }

    [Fact]
    public void Given_OverlappingIntervalsInList_When_UpdatingAvailability_Then_ThrowsInvalidOperationExceptionAndPreservesState()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);
        var initialSlot = new DateRange(new DateTime(2026, 11, 1, 9, 0, 0), new DateTime(2026, 11, 1, 10, 0, 0));
        tutor.AddAvailability(initialSlot);
        var overlappingSlots = new List<DateRange>
        {
            new(new DateTime(2026, 11, 2, 10, 0, 0), new DateTime(2026, 11, 2, 12, 0, 0)),
            new(new DateTime(2026, 11, 2, 11, 30, 0), new DateTime(2026, 11, 2, 13, 0, 0))
        };

        var action = () => tutor.UpdateAvailability(overlappingSlots);

        action.Should().Throw<InvalidOperationException>();
        tutor.Availability.Should().ContainSingle().Which.Should().Be(initialSlot);
    }

    [Fact]
    public void Given_AdjacentIntervals_When_UpdatingAvailability_Then_SucceedsWithoutOverlap()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);
        var adjacentSlots = new List<DateRange>
        {
            new(new DateTime(2026, 11, 2, 10, 0, 0), new DateTime(2026, 11, 2, 11, 0, 0)),
            new(new DateTime(2026, 11, 2, 11, 0, 0), new DateTime(2026, 11, 2, 12, 0, 0))
        };

        tutor.UpdateAvailability(adjacentSlots);

        tutor.Availability.Should().HaveCount(2);
        tutor.Availability.Should().ContainInOrder(adjacentSlots);
    }

    [Fact]
    public void Given_EmptyList_When_UpdatingAvailability_Then_ClearsAvailability()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);
        tutor.AddAvailability(new DateRange(new DateTime(2026, 11, 1, 9, 0, 0), new DateTime(2026, 11, 1, 10, 0, 0)));

        tutor.UpdateAvailability(new List<DateRange>());

        tutor.Availability.Should().BeEmpty();
    }

    [Fact]
    public void Given_NullList_When_UpdatingAvailability_Then_ThrowsArgumentNullException()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);

        var action = () => tutor.UpdateAvailability(null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Given_ValidInterval_When_AddingAvailability_Then_AddsSlotToCollection()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);
        var slot = new DateRange(new DateTime(2026, 11, 1, 10, 0, 0), new DateTime(2026, 11, 1, 12, 0, 0));

        tutor.AddAvailability(slot);

        tutor.Availability.Should().ContainSingle().Which.Should().Be(slot);
    }

    [Fact]
    public void Given_OverlappingInterval_When_AddingAvailability_Then_ThrowsInvalidOperationException()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);
        var existing = new DateRange(new DateTime(2026, 11, 1, 10, 0, 0), new DateTime(2026, 11, 1, 12, 0, 0));
        var overlap = new DateRange(new DateTime(2026, 11, 1, 11, 0, 0), new DateTime(2026, 11, 1, 13, 0, 0));
        tutor.AddAvailability(existing);

        var action = () => tutor.AddAvailability(overlap);

        action.Should().Throw<InvalidOperationException>();
        tutor.Availability.Should().ContainSingle().Which.Should().Be(existing);
    }

    [Fact]
    public void Given_NullInterval_When_AddingAvailability_Then_ThrowsArgumentNullException()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);

        var action = () => tutor.AddAvailability(null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Given_ExistingInterval_When_RemovingAvailability_Then_RemovesSlotAndReturnsTrue()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);
        var slot = new DateRange(new DateTime(2026, 11, 1, 10, 0, 0), new DateTime(2026, 11, 1, 12, 0, 0));
        tutor.AddAvailability(slot);

        var removed = tutor.RemoveAvailability(slot);

        removed.Should().BeTrue();
        tutor.Availability.Should().BeEmpty();
    }

    [Fact]
    public void Given_MissingInterval_When_RemovingAvailability_Then_ReturnsFalse()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);
        var slot = new DateRange(new DateTime(2026, 11, 1, 10, 0, 0), new DateTime(2026, 11, 1, 12, 0, 0));

        var removed = tutor.RemoveAvailability(slot);

        removed.Should().BeFalse();
    }

    [Fact]
    public void Given_NullInterval_When_RemovingAvailability_Then_ThrowsArgumentNullException()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);

        var action = () => tutor.RemoveAvailability(null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Given_MatchingInterval_When_CheckingIsAvailableDuring_Then_ReturnsTrue()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);
        tutor.AddAvailability(new DateRange(new DateTime(2026, 11, 1, 10, 0, 0), new DateTime(2026, 11, 1, 14, 0, 0)));
        var request = new DateRange(new DateTime(2026, 11, 1, 11, 0, 0), new DateTime(2026, 11, 1, 12, 0, 0));

        var result = tutor.IsAvailableDuring(request);

        result.Should().BeTrue();
    }

    [Fact]
    public void Given_NonMatchingInterval_When_CheckingIsAvailableDuring_Then_ReturnsFalse()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);
        tutor.AddAvailability(new DateRange(new DateTime(2026, 11, 1, 10, 0, 0), new DateTime(2026, 11, 1, 14, 0, 0)));
        var request = new DateRange(new DateTime(2026, 11, 1, 15, 0, 0), new DateTime(2026, 11, 1, 16, 0, 0));

        var result = tutor.IsAvailableDuring(request);

        result.Should().BeFalse();
    }

    [Fact]
    public void Given_NullRequestedRange_When_CheckingIsAvailableDuring_Then_ThrowsArgumentNullException()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);

        var action = () => tutor.IsAvailableDuring(null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Given_StartAfterEnd_When_ConstructingDateRange_Then_ThrowsArgumentException()
    {
        var start = new DateTime(2026, 11, 1, 14, 0, 0);
        var end = new DateTime(2026, 11, 1, 10, 0, 0);

        var action = () => new DateRange(start, end);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Given_StartEqualToEnd_When_ConstructingDateRange_Then_ThrowsArgumentException()
    {
        var time = new DateTime(2026, 11, 1, 10, 0, 0);

        var action = () => new DateRange(time, time);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Given_ValidDates_When_ConstructingDateRange_Then_InitializesStartAndEnd()
    {
        var start = new DateTime(2026, 11, 1, 10, 0, 0);
        var end = new DateTime(2026, 11, 1, 12, 0, 0);

        var range = new DateRange(start, end);

        range.Start.Should().Be(start);
        range.End.Should().Be(end);
        range.Duration.Should().Be(TimeSpan.FromHours(2));
    }

    [Fact]
    public void Given_DomainEvent_When_AddingDomainEvent_Then_StoresInDomainEvents()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);
        var notification = new TutorSavedNotification(tutor.Id, tutor.Bio, new List<string>());

        tutor.AddDomainEvent(notification);

        tutor.DomainEvents.Should().ContainSingle().Which.Should().Be(notification);
    }

    [Fact]
    public void Given_NullDomainEvent_When_AddingDomainEvent_Then_ThrowsArgumentNullException()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);

        var action = () => tutor.AddDomainEvent(null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Given_RecordedDomainEvents_When_ClearingDomainEvents_Then_EmptiesDomainEvents()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);
        var notification = new TutorSavedNotification(tutor.Id, tutor.Bio, new List<string>());
        tutor.AddDomainEvent(notification);

        tutor.ClearDomainEvents();

        tutor.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Given_TutorWithSubjects_When_CreatingTutorSavedNotification_Then_PopulatesTutorIdBioAndSubjects()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Student Bio", 80m);
        var subject = new Subject("Algorithms");
        tutor.AddSubject(subject);

        var notification = tutor.CreateTutorSavedNotification();

        notification.TutorId.Should().Be(tutor.Id);
        notification.Bio.Should().Be("Student Bio");
        notification.Subjects.Should().ContainSingle().Which.Should().Be("Algorithms");
    }

    [Fact]
    public void Given_ExplicitSubjectNames_When_CreatingTutorSavedNotification_Then_UsesExplicitNames()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Student Bio", 80m);
        var explicitNames = new List<string> { "Databases", "OOP" };

        var notification = tutor.CreateTutorSavedNotification(explicitNames);

        notification.TutorId.Should().Be(tutor.Id);
        notification.Bio.Should().Be("Student Bio");
        notification.Subjects.Should().Equal(explicitNames);
    }
}
