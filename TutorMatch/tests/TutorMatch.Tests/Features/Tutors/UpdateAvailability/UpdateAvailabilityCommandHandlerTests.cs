using FluentAssertions;
using TutorMatch.Api.Common.Dtos;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Domain.Tutors.Events;
using TutorMatch.Api.Features.Tutors.UpdateAvailability;
using Xunit;

namespace TutorMatch.Tests.Features.Tutors.UpdateAvailability;

public class UpdateAvailabilityCommandHandlerTests
{
    private sealed class FakeTutorRepository : ITutorRepository
    {
        public Action<Tutor>? UpdateBehavior { get; set; }
        private readonly Dictionary<Guid, Tutor> _tutors = new();

        public void Add(Tutor tutor)
        {
            _tutors[tutor.Id] = tutor;
        }

        public Tutor? GetById(Guid id)
        {
            return _tutors.TryGetValue(id, out var tutor) ? tutor : null;
        }

        public IReadOnlyList<Tutor> GetAll()
        {
            return _tutors.Values.ToList().AsReadOnly();
        }

        public void Update(Tutor tutor)
        {
            UpdateBehavior?.Invoke(tutor);
            _tutors[tutor.Id] = tutor;
        }

        public bool Delete(Guid id)
        {
            return _tutors.Remove(id);
        }
    }

    [Fact]
    public async Task Given_MissingTutor_When_HandlingUpdateAvailability_Then_ReturnsNotFoundError()
    {
        var repository = new FakeTutorRepository();
        var handler = new UpdateAvailabilityCommandHandler(repository);
        var command = new UpdateAvailabilityCommand(Guid.NewGuid(), new List<DateRangeDto>());

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Given_ValidAvailability_When_HandlingUpdateAvailability_Then_ReturnsSuccessAndUpdatesTutor()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);
        var repository = new FakeTutorRepository();
        repository.Add(tutor);
        var handler = new UpdateAvailabilityCommandHandler(repository);
        var start1 = new DateTime(2026, 11, 2, 10, 0, 0);
        var end1 = new DateTime(2026, 11, 2, 12, 0, 0);
        var start2 = new DateTime(2026, 11, 2, 14, 0, 0);
        var end2 = new DateTime(2026, 11, 2, 16, 0, 0);
        var command = new UpdateAvailabilityCommand(tutor.Id, new List<DateRangeDto>
        {
            new(start1, end1),
            new(start2, end2)
        });

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        var updated = repository.GetById(tutor.Id);
        updated!.Availability.Should().HaveCount(2);
        updated.Availability[0].Start.Should().Be(start1);
        updated.Availability[1].End.Should().Be(end2);
    }

    [Fact]
    public async Task Given_OverlappingAvailability_When_HandlingUpdateAvailability_Then_ReturnsValidationError()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);
        var initialSlot = new DateRange(new DateTime(2026, 11, 1, 9, 0, 0), new DateTime(2026, 11, 1, 10, 0, 0));
        tutor.AddAvailability(initialSlot);
        var repository = new FakeTutorRepository();
        repository.Add(tutor);
        var handler = new UpdateAvailabilityCommandHandler(repository);
        var command = new UpdateAvailabilityCommand(tutor.Id, new List<DateRangeDto>
        {
            new(new DateTime(2026, 11, 2, 10, 0, 0), new DateTime(2026, 11, 2, 12, 0, 0)),
            new(new DateTime(2026, 11, 2, 11, 0, 0), new DateTime(2026, 11, 2, 13, 0, 0))
        });

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        repository.GetById(tutor.Id)!.Availability.Should().ContainSingle().Which.Should().Be(initialSlot);
    }

    [Fact]
    public async Task Given_InvalidDateRange_When_HandlingUpdateAvailability_Then_ReturnsValidationError()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);
        var repository = new FakeTutorRepository();
        repository.Add(tutor);
        var handler = new UpdateAvailabilityCommandHandler(repository);
        var command = new UpdateAvailabilityCommand(tutor.Id, new List<DateRangeDto>
        {
            new(new DateTime(2026, 11, 2, 14, 0, 0), new DateTime(2026, 11, 2, 10, 0, 0))
        });

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Given_NullAvailability_When_HandlingUpdateAvailability_Then_ReturnsSuccessAndClearsAvailability()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);
        tutor.AddAvailability(new DateRange(new DateTime(2026, 11, 1, 9, 0, 0), new DateTime(2026, 11, 1, 10, 0, 0)));
        var repository = new FakeTutorRepository();
        repository.Add(tutor);
        var handler = new UpdateAvailabilityCommandHandler(repository);
        var command = new UpdateAvailabilityCommand(tutor.Id, null!);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        repository.GetById(tutor.Id)!.Availability.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_RepositoryKeyNotFoundException_When_HandlingUpdateAvailability_Then_ReturnsNotFoundError()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);
        var repository = new FakeTutorRepository
        {
            UpdateBehavior = _ => throw new KeyNotFoundException("Tutor not found during concurrent update")
        };
        repository.Add(tutor);
        var handler = new UpdateAvailabilityCommandHandler(repository);
        var command = new UpdateAvailabilityCommand(tutor.Id, new List<DateRangeDto>());

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Given_ValidAvailability_When_HandlingUpdateAvailability_Then_PublishesTutorSavedNotification()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);
        var repository = new FakeTutorRepository();
        repository.Add(tutor);
        var handler = new UpdateAvailabilityCommandHandler(repository);
        TutorSavedNotification? publishedNotification = null;
        Action<TutorSavedNotification> onPublished = n => publishedNotification = n;
        TutorSavedNotification.Handler += onPublished;
        var command = new UpdateAvailabilityCommand(tutor.Id, new List<DateRangeDto>());

        var result = await handler.HandleAsync(command);

        TutorSavedNotification.Handler -= onPublished;
        result.IsSuccess.Should().BeTrue();
        publishedNotification.Should().NotBeNull();
        publishedNotification!.TutorId.Should().Be(tutor.Id);
        publishedNotification.Bio.Should().Be("Bio");
    }
}
