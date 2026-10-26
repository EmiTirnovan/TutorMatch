using FluentAssertions;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Features.Tutors.CreateTutor;
using Xunit;

namespace TutorMatch.Tests.Features.Tutors.CreateTutor;

public class CreateTutorCommandHandlerTests
{
    private sealed class FakeTutorRepository : ITutorRepository
    {
        public Action<Tutor>? AddBehavior { get; set; }
        private readonly Dictionary<Guid, Tutor> _tutors = new();

        public void Add(Tutor tutor)
        {
            AddBehavior?.Invoke(tutor);
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
    public async Task Given_validCommand_When_handlingCreateTutor_Then_returnsSuccessfulResult()
    {
        var repository = new FakeTutorRepository();
        var handler = new CreateTutorCommandHandler(repository);
        var subjectId = Guid.NewGuid();
        var availability = new List<DateRangeDto>
        {
            new(new DateTime(2026, 10, 20, 10, 0, 0), new DateTime(2026, 10, 20, 12, 0, 0))
        };
        var command = new CreateTutorCommand("Emi", "emi@test.com", "Bio", 90m, new List<Guid>{ subjectId }, availability);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Emi");
        result.Value.Email.Should().Be("emi@test.com");
        result.Value.HourlyRate.Should().Be(90m);
        result.Value.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Given_invalidProfile_When_handlingCreateTutor_Then_returnsValidationError()
    {
        var repository = new FakeTutorRepository();
        var handler = new CreateTutorCommandHandler(repository);
        var command = new CreateTutorCommand("", "emi@test.com", "Bio", 90m);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Given_duplicateSubject_When_handlingCreateTutor_Then_returnsConflictError()
    {
        var repository = new FakeTutorRepository
        {
            AddBehavior = tutor => throw new InvalidOperationException("duplicate")
        };
        var handler = new CreateTutorCommandHandler(repository);
        var command = new CreateTutorCommand("Emi", "emi@test.com", "Bio", 90m, new List<Guid>{ Guid.NewGuid() });

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }
}
