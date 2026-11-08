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
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

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

        public Subject GetOrCreateSubject(string name) => new Subject(name);
    }

    [Fact]
    public async Task Given_ExistingTutorAndValidSlots_When_HandlingUpdateAvailability_Then_ReplacesAvailabilitySuccessfully()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);
        tutor.AddAvailability(new DateRange(new DateTime(2026, 11, 1, 9, 0, 0), new DateTime(2026, 11, 1, 10, 0, 0)));
        var repository = new FakeTutorRepository();
        repository.Add(tutor);
        var handler = new UpdateAvailabilityCommandHandler(repository);

        var newSlots = new List<DateRangeDto>
        {
            new(new DateTime(2026, 11, 2, 10, 0, 0), new DateTime(2026, 11, 2, 12, 0, 0)),
            new(new DateTime(2026, 11, 2, 14, 0, 0), new DateTime(2026, 11, 2, 16, 0, 0))
        };
        var command = new UpdateAvailabilityCommand(tutor.Id, newSlots);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        var updated = repository.GetById(tutor.Id)!;
        updated.Availability.Should().HaveCount(2);
        updated.Availability.Should().Contain(r => r.Start == new DateTime(2026, 11, 2, 10, 0, 0));
        updated.Availability.Should().Contain(r => r.Start == new DateTime(2026, 11, 2, 14, 0, 0));
        updated.Availability.Should().NotContain(r => r.Start == new DateTime(2026, 11, 1, 9, 0, 0));
    }

    [Fact]
    public async Task Given_NonExistingTutor_When_HandlingUpdateAvailability_Then_ReturnsNotFoundError()
    {
        var repository = new FakeTutorRepository();
        var handler = new UpdateAvailabilityCommandHandler(repository);
        var command = new UpdateAvailabilityCommand(Guid.NewGuid(), new List<DateRangeDto>());

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Given_OverlappingSlotsInCommand_When_HandlingUpdateAvailability_Then_ReturnsValidationError()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);
        var repository = new FakeTutorRepository();
        repository.Add(tutor);
        var handler = new UpdateAvailabilityCommandHandler(repository);

        var overlappingSlots = new List<DateRangeDto>
        {
            new(new DateTime(2026, 11, 2, 10, 0, 0), new DateTime(2026, 11, 2, 12, 0, 0)),
            new(new DateTime(2026, 11, 2, 11, 0, 0), new DateTime(2026, 11, 2, 13, 0, 0))
        };
        var command = new UpdateAvailabilityCommand(tutor.Id, overlappingSlots);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Given_InvalidDateRange_When_HandlingUpdateAvailability_Then_ReturnsValidationError()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);
        var repository = new FakeTutorRepository();
        repository.Add(tutor);
        var handler = new UpdateAvailabilityCommandHandler(repository);

        var invalidSlots = new List<DateRangeDto>
        {
            new(new DateTime(2026, 11, 2, 12, 0, 0), new DateTime(2026, 11, 2, 10, 0, 0))
        };
        var command = new UpdateAvailabilityCommand(tutor.Id, invalidSlots);

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
        await Semaphore.WaitAsync();
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);
        var repository = new FakeTutorRepository();
        repository.Add(tutor);
        var handler = new UpdateAvailabilityCommandHandler(repository);
        TutorSavedNotification? publishedNotification = null;
        Action<TutorSavedNotification> onPublished = n =>
        {
            if (n.TutorId == tutor.Id)
            {
                publishedNotification = n;
            }
        };
        TutorSavedNotification.Handler += onPublished;
        try
        {
            var command = new UpdateAvailabilityCommand(tutor.Id, new List<DateRangeDto>());
            var result = await handler.HandleAsync(command);

            result.IsSuccess.Should().BeTrue();
            publishedNotification.Should().NotBeNull();
            publishedNotification!.TutorId.Should().Be(tutor.Id);
            publishedNotification.Bio.Should().Be("Bio");
        }
        finally
        {
            TutorSavedNotification.Handler -= onPublished;
            Semaphore.Release();
        }
    }

    [Fact]
    public async Task Given_DatabaseSaveFails_When_HandlingUpdateAvailability_Then_DoesNotPublishTutorSavedNotification()
    {
        await Semaphore.WaitAsync();
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 80m);
        var repository = new FakeTutorRepository
        {
            UpdateBehavior = _ => throw new InvalidOperationException("Simulated database failure on update")
        };
        repository.Add(tutor);
        var handler = new UpdateAvailabilityCommandHandler(repository);
        TutorSavedNotification? publishedNotification = null;
        Action<TutorSavedNotification> onPublished = n =>
        {
            if (n.TutorId == tutor.Id)
            {
                publishedNotification = n;
            }
        };
        TutorSavedNotification.Handler += onPublished;
        try
        {
            var command = new UpdateAvailabilityCommand(tutor.Id, new List<DateRangeDto>());
            var result = await handler.HandleAsync(command);

            result.IsSuccess.Should().BeFalse();
            publishedNotification.Should().BeNull();
        }
        finally
        {
            TutorSavedNotification.Handler -= onPublished;
            Semaphore.Release();
        }
    }
}
