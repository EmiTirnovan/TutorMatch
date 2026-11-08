using System.Collections.Concurrent;
using FluentAssertions;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Domain.Students;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Features.Sessions.BookSession;
using Xunit;

namespace TutorMatch.Tests.Features.Sessions.BookSession;

public class BookSessionConcurrencyTests
{
    private sealed class ThreadSafeSessionRepository : ISessionRepository
    {
        private readonly ConcurrentDictionary<Guid, Session> _sessions = new();

        public void Add(Session session)
        {
            if (!_sessions.TryAdd(session.Id, session))
            {
                throw new InvalidOperationException($"Session with ID '{session.Id}' already exists.");
            }
        }

        public Session? GetById(Guid id) =>
            _sessions.TryGetValue(id, out var session) ? session : null;

        public IReadOnlyList<Session> GetAll() =>
            _sessions.Values.ToList().AsReadOnly();

        public void Update(Session session) =>
            _sessions[session.Id] = session;

        public bool Delete(Guid id) =>
            _sessions.TryRemove(id, out _);
    }

    private sealed class ThreadSafeTutorRepository : ITutorRepository
    {
        private readonly ConcurrentDictionary<Guid, Tutor> _tutors = new();

        public void Add(Tutor tutor) =>
            _tutors[tutor.Id] = tutor;

        public Tutor? GetById(Guid id) =>
            _tutors.TryGetValue(id, out var tutor) ? tutor : null;

        public IReadOnlyList<Tutor> GetAll() =>
            _tutors.Values.ToList().AsReadOnly();

        public void Update(Tutor tutor) =>
            _tutors[tutor.Id] = tutor;

        public bool Delete(Guid id) =>
            _tutors.TryRemove(id, out _);

        public Subject GetOrCreateSubject(string name) => new Subject(name);
    }

    private sealed class ThreadSafeStudentRepository : IStudentRepository
    {
        private readonly ConcurrentDictionary<Guid, Student> _students = new();

        public void Add(Student student) =>
            _students[student.Id] = student;

        public Student? GetById(Guid id) =>
            _students.TryGetValue(id, out var student) ? student : null;

        public IReadOnlyList<Student> GetAll() =>
            _students.Values.ToList().AsReadOnly();

        public void Update(Student student) =>
            _students[student.Id] = student;

        public bool Delete(Guid id) =>
            _students.TryRemove(id, out _);
    }

    [Fact]
    public async Task Given_twoConcurrentBookingRequestsOnSameSlot_When_runningInParallel_Then_exactlyOneSucceedsAndOneReturnsConflict()
    {
        var sessionRepo = new ThreadSafeSessionRepository();
        var tutorRepo = new ThreadSafeTutorRepository();
        var studentRepo = new ThreadSafeStudentRepository();
        var handler = new BookSessionCommandHandler(sessionRepo, tutorRepo, studentRepo);
        var tutorId = Guid.NewGuid();
        var student1Id = Guid.NewGuid();
        var student2Id = Guid.NewGuid();
        var start = new DateTime(2026, 11, 2, 10, 0, 0);
        var end = new DateTime(2026, 11, 2, 11, 0, 0);
        var tutor = new Tutor("Tutor", "tutor@test.com", "Bio", 50, tutorId);
        tutor.AddAvailability(new DateRange(start, end));
        tutorRepo.Add(tutor);
        var student1 = new Student(student1Id, "Student 1", "student1@test.com");
        var student2 = new Student(student2Id, "Student 2", "student2@test.com");
        studentRepo.Add(student1);
        studentRepo.Add(student2);
        var command1 = new BookSessionCommand(tutorId, student1Id, start, end);
        var command2 = new BookSessionCommand(tutorId, student2Id, start, end);

        var task1 = handler.HandleAsync(command1);
        var task2 = handler.HandleAsync(command2);
        var results = await Task.WhenAll(task1, task2);

        results.Count(r => r.IsSuccess).Should().Be(1);
        results.Count(r => r.IsFailure && r.Error.Type == ErrorType.Conflict).Should().Be(1);
        sessionRepo.GetAll().Should().HaveCount(1);
    }

    [Fact]
    public async Task Given_twoConcurrentBookingRequestsForSameStudentOnOverlappingSlots_When_runningInParallel_Then_exactlyOneSucceedsAndOneReturnsConflict()
    {
        var sessionRepo = new ThreadSafeSessionRepository();
        var tutorRepo = new ThreadSafeTutorRepository();
        var studentRepo = new ThreadSafeStudentRepository();
        var handler = new BookSessionCommandHandler(sessionRepo, tutorRepo, studentRepo);
        var tutor1Id = Guid.NewGuid();
        var tutor2Id = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var start = new DateTime(2026, 11, 2, 10, 0, 0);
        var end = new DateTime(2026, 11, 2, 11, 0, 0);
        var tutor1 = new Tutor("Tutor 1", "tutor1@test.com", "Bio", 50, tutor1Id);
        var tutor2 = new Tutor("Tutor 2", "tutor2@test.com", "Bio", 60, tutor2Id);
        tutor1.AddAvailability(new DateRange(start, end));
        tutor2.AddAvailability(new DateRange(start, end));
        tutorRepo.Add(tutor1);
        tutorRepo.Add(tutor2);
        var student = new Student(studentId, "Student", "student@test.com");
        studentRepo.Add(student);
        var command1 = new BookSessionCommand(tutor1Id, studentId, start, end);
        var command2 = new BookSessionCommand(tutor2Id, studentId, start, end);

        var task1 = handler.HandleAsync(command1);
        var task2 = handler.HandleAsync(command2);
        var results = await Task.WhenAll(task1, task2);

        results.Count(r => r.IsSuccess).Should().Be(1);
        results.Count(r => r.IsFailure && r.Error.Type == ErrorType.Conflict).Should().Be(1);
        sessionRepo.GetAll().Should().HaveCount(1);
    }
}
