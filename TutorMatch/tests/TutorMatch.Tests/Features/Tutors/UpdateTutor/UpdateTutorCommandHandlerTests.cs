using FluentAssertions;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Features.Tutors.UpdateTutor;
using Xunit;

namespace TutorMatch.Tests.Features.Tutors.UpdateTutor;

public class UpdateTutorCommandHandlerTests
{
    private sealed class FakeTutorRepository : ITutorRepository
    {
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
            _tutors[tutor.Id] = tutor;
        }

        public bool Delete(Guid id)
        {
            return _tutors.Remove(id);
        }
    }

    [Fact]
    public async Task Given_missingTutor_When_handlingUpdateTutor_Then_returnsNotFoundError()
    {
        var repository = new FakeTutorRepository();
        var handler = new UpdateTutorCommandHandler(repository);

        var result = await handler.HandleAsync(new UpdateTutorCommand(Guid.NewGuid(), "Emi", "Bio", 90m));

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Given_validTutor_When_handlingUpdateTutor_Then_returnsSuccess()
    {
        var tutor = new Tutor("Old", "old@test.com", "Old bio", 50m);
        var repository = new FakeTutorRepository();
        repository.Add(tutor);
        var handler = new UpdateTutorCommandHandler(repository);

        var result = await handler.HandleAsync(new UpdateTutorCommand(tutor.Id, "New", "New bio", 75m));

        result.IsSuccess.Should().BeTrue();
        repository.GetById(tutor.Id)!.Name.Should().Be("New");
        repository.GetById(tutor.Id)!.HourlyRate.Should().Be(75m);
    }

    [Fact]
    public async Task Given_validProfileChange_When_handlingUpdateTutor_Then_updatesAggregateStateAndPersistsIt()
    {
        var tutor = new Tutor("Old", "old@test.com", "Old bio", 50m);
        var repository = new FakeTutorRepository();
        repository.Add(tutor);
        var handler = new UpdateTutorCommandHandler(repository);

        var result = await handler.HandleAsync(new UpdateTutorCommand(tutor.Id, "New", "New bio", 75m));

        result.IsSuccess.Should().BeTrue();
        tutor.Name.Should().Be("New");
        tutor.Bio.Should().Be("New bio");
        tutor.HourlyRate.Should().Be(75m);
        repository.GetById(tutor.Id)!.Name.Should().Be("New");
        repository.GetById(tutor.Id)!.Bio.Should().Be("New bio");
        repository.GetById(tutor.Id)!.HourlyRate.Should().Be(75m);
    }

    [Fact]
    public async Task Given_repositoryUpdateFailure_When_handlingUpdateTutor_Then_returnsConflictError()
    {
        var tutor = new Tutor("Old", "old@test.com", "Old bio", 50m);
        var repository = new ThrowingOnUpdateTutorRepository(tutor);
        var handler = new UpdateTutorCommandHandler(repository);

        var result = await handler.HandleAsync(new UpdateTutorCommand(tutor.Id, "New", "New bio", 75m));

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Description.Should().Contain("update");
    }

    [Fact]
    public async Task Given_invalidProfile_When_handlingUpdateTutor_Then_returnsValidationError()
    {
        var tutor = new Tutor("Old", "old@test.com", "Old bio", 50m);
        var repository = new FakeTutorRepository();
        repository.Add(tutor);
        var handler = new UpdateTutorCommandHandler(repository);

        var result = await handler.HandleAsync(new UpdateTutorCommand(tutor.Id, "", "New bio", 75m));

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    private sealed class ThrowingOnUpdateTutorRepository : ITutorRepository
    {
        private readonly Tutor _tutor;

        public ThrowingOnUpdateTutorRepository(Tutor tutor)
        {
            _tutor = tutor;
        }

        public void Add(Tutor tutor)
        {
        }

        public Tutor? GetById(Guid id)
        {
            return _tutor.Id == id ? _tutor : null;
        }

        public IReadOnlyList<Tutor> GetAll()
        {
            return new[] { _tutor };
        }

        public void Update(Tutor tutor)
        {
            throw new InvalidOperationException("update failed");
        }

        public bool Delete(Guid id)
        {
            return false;
        }
    }
}
