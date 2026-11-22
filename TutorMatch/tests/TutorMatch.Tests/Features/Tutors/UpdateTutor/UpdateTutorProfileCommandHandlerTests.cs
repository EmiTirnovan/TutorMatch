using FluentAssertions;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Domain.Tutors.Events;
using TutorMatch.Api.Features.Tutors.UpdateTutor;
using Xunit;

namespace TutorMatch.Tests.Features.Tutors.UpdateTutor;

public class UpdateTutorProfileCommandHandlerTests
{
    private sealed class FakeTutorRepo : ITutorRepository
    {
        private readonly Dictionary<Guid, Tutor> _tutors = new();
        private readonly Dictionary<string, Subject> _subjects = new(StringComparer.OrdinalIgnoreCase);

        public void Add(Tutor tutor) => _tutors[tutor.Id] = tutor;
        public Tutor? GetById(Guid id) => _tutors.TryGetValue(id, out var t) ? t : null;
        public IReadOnlyList<Tutor> GetAll() => _tutors.Values.ToList().AsReadOnly();
        public void Update(Tutor tutor) => _tutors[tutor.Id] = tutor;
        public bool Delete(Guid id) => _tutors.Remove(id);
        public Subject GetOrCreateSubject(string name)
        {
            var norm = Subject.NormalizeName(name);
            if (!_subjects.TryGetValue(norm, out var s))
            {
                s = new Subject(norm);
                _subjects[norm] = s;
            }
            return s;
        }
    }

    [Fact]
    public async Task Given_EmptyName_When_UpdatingProfile_Then_ReturnsValidationError()
    {
        var repo = new FakeTutorRepo();
        var handler = new UpdateTutorCommandHandler(repo);
        var tutor = new Tutor("Tutor", "t@test.com", "Bio", 80m);
        repo.Add(tutor);

        var result = await handler.HandleAsync(new UpdateTutorProfileCommand(tutor.Id, "", "Bio", 80m));

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task Given_InvalidHourlyRate_When_UpdatingProfile_Then_ReturnsValidationError(decimal invalidRate)
    {
        var repo = new FakeTutorRepo();
        var handler = new UpdateTutorCommandHandler(repo);
        var tutor = new Tutor("Tutor", "t@test.com", "Bio", 80m);
        repo.Add(tutor);

        var result = await handler.HandleAsync(new UpdateTutorProfileCommand(tutor.Id, "Valid Name", "Bio", invalidRate));

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Given_NullBio_When_UpdatingProfile_Then_ReturnsValidationError()
    {
        var repo = new FakeTutorRepo();
        var handler = new UpdateTutorCommandHandler(repo);
        var tutor = new Tutor("Tutor", "t@test.com", "Bio", 80m);
        repo.Add(tutor);

        var result = await handler.HandleAsync(new UpdateTutorProfileCommand(tutor.Id, "Valid Name", null!, 80m));

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Given_NonexistentTutor_When_UpdatingProfile_Then_ReturnsNotFoundError()
    {
        var repo = new FakeTutorRepo();
        var handler = new UpdateTutorCommandHandler(repo);

        var result = await handler.HandleAsync(new UpdateTutorProfileCommand(Guid.NewGuid(), "Valid Name", "Valid bio", 80m));

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Given_ValidCommandWithSubjects_When_UpdatingProfile_Then_NormalizesAndDeduplicatesSubjectsAndPublishesEvents()
    {
        var repo = new FakeTutorRepo();
        var handler = new UpdateTutorCommandHandler(repo);
        var tutor = new Tutor("Tutor", "t@test.com", "Old bio", 80m);
        repo.Add(tutor);

        TutorProfileUpdated? profileUpdatedEvent = null;
        TutorSavedNotification? aiSyncNotification = null;

        Action<TutorProfileUpdated> onProfileUpdated = e => { if (e.TutorId == tutor.Id) profileUpdatedEvent = e; };
        Action<TutorSavedNotification> onAiSync = n => { if (n.TutorId == tutor.Id) aiSyncNotification = n; };

        TutorProfileUpdated.Handler += onProfileUpdated;
        TutorSavedNotification.Handler += onAiSync;

        try
        {
            var command = new UpdateTutorProfileCommand(
                tutor.Id,
                "New Name",
                "New Bio and experience",
                120m,
                new List<string> { "  Java  ", "java", "C++", "  c++  ", "Algorithms" }
            );

            var result = await handler.HandleAsync(command);

            result.IsSuccess.Should().BeTrue();

            var updated = repo.GetById(tutor.Id)!;
            updated.Name.Should().Be("New Name");
            updated.Bio.Should().Be("New Bio and experience");
            updated.HourlyRate.Should().Be(120m);

            // Deduplication: Java, C++, Algorithms (3 distinct subjects)
            updated.TutorSubjects.Should().HaveCount(3);
            updated.TutorSubjects.Select(ts => ts.Subject.Name).Should().Contain("Java");
            updated.TutorSubjects.Select(ts => ts.Subject.Name).Should().Contain("C++");
            updated.TutorSubjects.Select(ts => ts.Subject.Name).Should().Contain("Algorithms");

            // Verify TutorProfileUpdated was published
            profileUpdatedEvent.Should().NotBeNull();
            profileUpdatedEvent!.TutorId.Should().Be(tutor.Id);
            profileUpdatedEvent.Bio.Should().Be("New Bio and experience");
            profileUpdatedEvent.Subjects.Should().Contain(new[] { "Java", "C++", "Algorithms" });

            // Verify AI embedding sync was triggered
            aiSyncNotification.Should().NotBeNull();
            aiSyncNotification!.TutorId.Should().Be(tutor.Id);
        }
        finally
        {
            TutorProfileUpdated.Handler -= onProfileUpdated;
            TutorSavedNotification.Handler -= onAiSync;
        }
    }
}
