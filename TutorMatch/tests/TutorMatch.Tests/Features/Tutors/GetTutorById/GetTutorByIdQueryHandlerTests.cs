using FluentAssertions;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Features.Tutors.GetTutorById;
using Xunit;

namespace TutorMatch.Tests.Features.Tutors.GetTutorById;

public class GetTutorByIdQueryHandlerTests
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
    public async Task Given_existingTutor_When_handlingGetTutorById_Then_returnsTutorResponse()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 90m);
        tutor.AddAvailability(new DateRange(new DateTime(2026, 10, 26, 10, 0, 0), new DateTime(2026, 10, 26, 12, 0, 0)));
        var subject = new Subject("Algoritmi");
        tutor.AddSubject(subject);
        var repository = new FakeTutorRepository();
        repository.Add(tutor);
        var handler = new GetTutorByIdQueryHandler(repository);

        var result = await handler.HandleAsync(new GetTutorByIdQuery(tutor.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(tutor.Id);
        result.Value.Name.Should().Be("Emi");
        result.Value.SubjectIds.Should().Contain(subject.Id);
        result.Value.Availability.Should().HaveCount(1);
    }

    [Fact]
    public async Task Given_missingTutor_When_handlingGetTutorById_Then_returnsNotFoundError()
    {
        var repository = new FakeTutorRepository();
        var handler = new GetTutorByIdQueryHandler(repository);

        var result = await handler.HandleAsync(new GetTutorByIdQuery(Guid.NewGuid()));

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }
}
