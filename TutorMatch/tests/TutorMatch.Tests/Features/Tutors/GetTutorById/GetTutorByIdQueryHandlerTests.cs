using FluentAssertions;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Domain.Students;
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

        public Subject GetOrCreateSubject(string name) => new Subject(name);
    }

    private static FakeRatingRepository CreateRatings(Guid tutorId, params int[] scores)
    {
        var repository = new FakeRatingRepository();
        scores.Select(score => new Rating(Guid.NewGuid(), tutorId, Guid.NewGuid(), score)).ToList().ForEach(repository.Add);
        return repository;
    }
    private sealed class FakeRatingRepository : IRatingRepository
    {
        private readonly List<Rating> _ratings = new();

        public void Add(Rating rating)
        {
            _ratings.Add(rating);
        }

        public IReadOnlyList<Rating> GetByTutorId(Guid tutorId)
        {
            return _ratings.Where(r => r.TutorId == tutorId).ToList().AsReadOnly();
        }

        public IReadOnlyList<Rating> GetBySessionIds(IEnumerable<Guid> sessionIds)
        {
            var ids = sessionIds.ToHashSet();
            return _ratings.Where(r => ids.Contains(r.SessionId)).ToList().AsReadOnly();
        }
    }

    private sealed class FakeSessionRepository : ISessionRepository
    {
        private readonly Dictionary<Guid, Session> _sessions = new();

        public void Add(Session session)
        {
            _sessions[session.Id] = session;
        }

        public Session? GetById(Guid id)
        {
            return _sessions.TryGetValue(id, out var session) ? session : null;
        }

        public IReadOnlyList<Session> GetAll()
        {
            return _sessions.Values.ToList().AsReadOnly();
        }

        public void Update(Session session)
        {
            _sessions[session.Id] = session;
        }

        public bool Delete(Guid id)
        {
            return _sessions.Remove(id);
        }
    }

    private sealed class FakeStudentRepository : IStudentRepository
    {
        private readonly Dictionary<Guid, Student> _students = new();

        public void Add(Student student)
        {
            _students[student.Id] = student;
        }

        public Student? GetById(Guid id)
        {
            return _students.TryGetValue(id, out var student) ? student : null;
        }

        public IReadOnlyList<Student> GetAll()
        {
            return _students.Values.ToList().AsReadOnly();
        }

        public void Update(Student student)
        {
            _students[student.Id] = student;
        }

        public bool Delete(Guid id)
        {
            return _students.Remove(id);
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

    [Fact]
    public async Task Given_tutorWithRatings_When_handlingGetTutorById_Then_returnsMathematicalAverageAndCount()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 90m);
        var tutorRepository = new FakeTutorRepository();
        tutorRepository.Add(tutor);
        var ratingRepository = new FakeRatingRepository();
        ratingRepository.Add(new Rating(Guid.NewGuid(), tutor.Id, Guid.NewGuid(), 5));
        ratingRepository.Add(new Rating(Guid.NewGuid(), tutor.Id, Guid.NewGuid(), 4));
        ratingRepository.Add(new Rating(Guid.NewGuid(), tutor.Id, Guid.NewGuid(), 4));
        var handler = new GetTutorByIdQueryHandler(tutorRepository, ratingRepository);

        var result = await handler.HandleAsync(new GetTutorByIdQuery(tutor.Id));

        result.Value.AverageRating.Should().BeApproximately(13d / 3d, 0.0001);
        result.Value.RatingsCount.Should().Be(3);
    }

    [Fact]
    public async Task Given_tutorWithoutRatings_When_handlingGetTutorById_Then_returnsZeroAverageEmptyReviewsAndZeroedDistribution()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 90m);
        var tutorRepository = new FakeTutorRepository();
        tutorRepository.Add(tutor);
        var handler = new GetTutorByIdQueryHandler(tutorRepository, new FakeRatingRepository());

        var result = await handler.HandleAsync(new GetTutorByIdQuery(tutor.Id));

        result.Value.AverageRating.Should().Be(0);
        result.Value.RatingsCount.Should().Be(0);
        result.Value.Reviews.Should().BeEmpty();
        result.Value.RatingDistribution.Should().Equal(new Dictionary<int, int> { [1] = 0, [2] = 0, [3] = 0, [4] = 0, [5] = 0 });
    }

    [Fact]
    public async Task Given_noRatingRepository_When_handlingGetTutorById_Then_returnsNoRatingData()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 90m);
        var tutorRepository = new FakeTutorRepository();
        tutorRepository.Add(tutor);
        var handler = new GetTutorByIdQueryHandler(tutorRepository);

        var result = await handler.HandleAsync(new GetTutorByIdQuery(tutor.Id));

        result.Value.RatingsCount.Should().Be(0);
        result.Value.Reviews.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_ratingsOfOtherTutors_When_handlingGetTutorById_Then_ignoresThem()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 90m);
        var tutorRepository = new FakeTutorRepository();
        tutorRepository.Add(tutor);
        var ratingRepository = new FakeRatingRepository();
        ratingRepository.Add(new Rating(Guid.NewGuid(), tutor.Id, Guid.NewGuid(), 2));
        ratingRepository.Add(new Rating(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 5));
        var handler = new GetTutorByIdQueryHandler(tutorRepository, ratingRepository);

        var result = await handler.HandleAsync(new GetTutorByIdQuery(tutor.Id));

        result.Value.AverageRating.Should().Be(2);
        result.Value.RatingsCount.Should().Be(1);
    }

    [Fact]
    public async Task Given_ratingsWithDifferentScores_When_handlingGetTutorById_Then_returnsCountPerStar()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 90m);
        var tutorRepository = new FakeTutorRepository();
        tutorRepository.Add(tutor);
        var ratingRepository = CreateRatings(tutor.Id, 5, 5, 5, 4, 1);
        var handler = new GetTutorByIdQueryHandler(tutorRepository, ratingRepository);

        var result = await handler.HandleAsync(new GetTutorByIdQuery(tutor.Id));

        result.Value.RatingDistribution.Should().Equal(new Dictionary<int, int> { [1] = 1, [2] = 0, [3] = 0, [4] = 1, [5] = 3 });
    }

    [Fact]
    public async Task Given_ratedSessions_When_handlingGetTutorById_Then_returnsReviewsNewestSessionFirstWithStudentName()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 90m);
        var tutorRepository = new FakeTutorRepository();
        tutorRepository.Add(tutor);
        var student = new Student(Guid.NewGuid(), "Alex Radu", "alex@test.com");
        var studentRepository = new FakeStudentRepository();
        studentRepository.Add(student);
        var olderSession = new Session(Guid.NewGuid(), tutor.Id, student.Id, new DateRange(new DateTime(2026, 10, 1, 10, 0, 0), new DateTime(2026, 10, 1, 11, 0, 0)), SessionStatus.Completed);
        var newerSession = new Session(Guid.NewGuid(), tutor.Id, student.Id, new DateRange(new DateTime(2026, 10, 5, 10, 0, 0), new DateTime(2026, 10, 5, 11, 0, 0)), SessionStatus.Completed);
        var sessionRepository = new FakeSessionRepository();
        sessionRepository.Add(olderSession);
        sessionRepository.Add(newerSession);
        var ratingRepository = new FakeRatingRepository();
        ratingRepository.Add(new Rating(olderSession.Id, tutor.Id, student.Id, 3, "Ok"));
        ratingRepository.Add(new Rating(newerSession.Id, tutor.Id, student.Id, 5, "Excelent"));
        var handler = new GetTutorByIdQueryHandler(tutorRepository, ratingRepository, sessionRepository, studentRepository);

        var result = await handler.HandleAsync(new GetTutorByIdQuery(tutor.Id));

        result.Value.Reviews.Should().HaveCount(2);
        result.Value.Reviews![0].Should().Be(new TutorReviewDto(newerSession.Id, new DateTime(2026, 10, 5, 10, 0, 0), 5, "Excelent", "Alex Radu"));
        result.Value.Reviews[1].Should().Be(new TutorReviewDto(olderSession.Id, new DateTime(2026, 10, 1, 10, 0, 0), 3, "Ok", "Alex Radu"));
    }

    [Fact]
    public async Task Given_ratingWithoutKnownSessionOrStudent_When_handlingGetTutorById_Then_usesRatingDateAndNullStudentName()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 90m);
        var tutorRepository = new FakeTutorRepository();
        tutorRepository.Add(tutor);
        var rating = new Rating(Guid.NewGuid(), tutor.Id, Guid.NewGuid(), 4);
        var ratingRepository = new FakeRatingRepository();
        ratingRepository.Add(rating);
        var handler = new GetTutorByIdQueryHandler(tutorRepository, ratingRepository, new FakeSessionRepository(), new FakeStudentRepository());

        var result = await handler.HandleAsync(new GetTutorByIdQuery(tutor.Id));

        result.Value.Reviews.Should().ContainSingle();
        result.Value.Reviews![0].Date.Should().Be(rating.CreatedAt);
        result.Value.Reviews[0].StudentName.Should().BeNull();
        result.Value.Reviews[0].Comment.Should().BeNull();
    }

    [Fact]
    public async Task Given_moreRatingsThanTheLimit_When_handlingGetTutorById_Then_limitsReviewsButCountsAllRatings()
    {
        var tutor = new Tutor("Emi", "emi@test.com", "Bio", 90m);
        var tutorRepository = new FakeTutorRepository();
        tutorRepository.Add(tutor);
        var ratingRepository = CreateRatings(tutor.Id, Enumerable.Repeat(5, GetTutorByIdQueryHandler.MaxRecentReviews + 5).ToArray());
        var handler = new GetTutorByIdQueryHandler(tutorRepository, ratingRepository);

        var result = await handler.HandleAsync(new GetTutorByIdQuery(tutor.Id));

        result.Value.Reviews.Should().HaveCount(GetTutorByIdQueryHandler.MaxRecentReviews);
        result.Value.RatingsCount.Should().Be(GetTutorByIdQueryHandler.MaxRecentReviews + 5);
        result.Value.RatingDistribution![5].Should().Be(GetTutorByIdQueryHandler.MaxRecentReviews + 5);
    }
}