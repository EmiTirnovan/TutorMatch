using FluentAssertions;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Features.Tutors.GetTutors;
using Xunit;

namespace TutorMatch.Tests.Features.Tutors.GetTutors;

public class GetTutorsQueryHandlerTests
{
    private sealed class FakeTutorRepository : ITutorRepository
    {
        private readonly List<Tutor> _tutors = new();

        public void Add(Tutor tutor)
        {
            _tutors.Add(tutor);
        }

        public Tutor? GetById(Guid id)
        {
            return _tutors.FirstOrDefault(t => t.Id == id);
        }

        public IReadOnlyList<Tutor> GetAll()
        {
            return _tutors.AsReadOnly();
        }

        public void Update(Tutor tutor)
        {
            var index = _tutors.FindIndex(t => t.Id == tutor.Id);
            if (index >= 0)
            {
                _tutors[index] = tutor;
            }
        }

        public bool Delete(Guid id)
        {
            var tutor = _tutors.FirstOrDefault(t => t.Id == id);
            if (tutor is null)
            {
                return false;
            }

            _tutors.Remove(tutor);
            return true;
        }
    }

    [Fact]
    public async Task Given_tutorsExist_When_handlingGetTutors_Then_returnsMappedSummaries()
    {
        var tutorOne = new Tutor("Emi", "emi@test.com", "Bio 1", 90m);
        var tutorTwo = new Tutor("Ana", "ana@test.com", "Bio 2", 120m);
        var repository = new FakeTutorRepository();
        repository.Add(tutorOne);
        repository.Add(tutorTwo);
        var handler = new GetTutorsQueryHandler(repository);

        var result = await handler.HandleAsync(new GetTutorsQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Id.Should().Be(tutorOne.Id);
        result.Value[1].Name.Should().Be("Ana");
    }
}
