using FluentAssertions;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Features.Tutors.DeleteTutor;
using Xunit;

namespace TutorMatch.Tests.Features.Tutors.DeleteTutor;

public class DeleteTutorCommandHandlerTests
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
    public async Task Given_existingTutor_When_handlingDeleteTutor_Then_returnsSuccess()
    {
        var repository = new FakeTutorRepository();
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 90m);
        repository.Add(tutor);
        var handler = new DeleteTutorCommandHandler(repository);

        var result = await handler.HandleAsync(new DeleteTutorCommand(tutor.Id));

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public async Task Given_missingTutor_When_handlingDeleteTutor_Then_returnsNotFoundError()
    {
        var repository = new FakeTutorRepository();
        var handler = new DeleteTutorCommandHandler(repository);

        var result = await handler.HandleAsync(new DeleteTutorCommand(Guid.NewGuid()));

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }
}
