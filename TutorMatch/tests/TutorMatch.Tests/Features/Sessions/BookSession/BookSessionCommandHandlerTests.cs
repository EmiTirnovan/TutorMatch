using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Domain.Students;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Features.Sessions.BookSession;
using Xunit;

namespace TutorMatch.Tests.Features.Sessions.BookSession;

public class BookSessionCommandHandlerTests
{
    private sealed class FakeSessionRepository : ISessionRepository
    {
        public Action<Session>? AddBehavior { get; set; }
        private readonly Dictionary<Guid, Session> _sessions = new();

        public void Add(Session session)
        {
            AddBehavior?.Invoke(session);
            _sessions[session.Id] = session;
        }

        public Session? GetById(Guid id) =>
            _sessions.TryGetValue(id, out var session) ? session : null;

        public IReadOnlyList<Session> GetAll() =>
            _sessions.Values.ToList().AsReadOnly();

        public void Update(Session session) =>
            _sessions[session.Id] = session;

        public bool Delete(Guid id) =>
            _sessions.Remove(id);
    }

    private sealed class FakeTutorRepository : ITutorRepository
    {
        private readonly Dictionary<Guid, Tutor> _tutors = new();

        public void Add(Tutor tutor) =>
            _tutors[tutor.Id] = tutor;

        public Tutor? GetById(Guid id) =>
            _tutors.TryGetValue(id, out var tutor) ? tutor : null;

        public IReadOnlyList<Tutor> GetAll() =>
            _tutors.Values.ToList().AsReadOnly();

        public void Update(Tutor tutor) =>
            _tutors[tutor.Id] = tutor;

        public bool Delete(Guid id) =>
            _tutors.Remove(id);
    }

    private sealed class FakeStudentRepository : IStudentRepository
    {
        public Action<Student>? AddBehavior { get; set; }
        private readonly Dictionary<Guid, Student> _students = new();

        public void Add(Student student)
        {
            AddBehavior?.Invoke(student);
            _students[student.Id] = student;
        }

        public Student? GetById(Guid id) =>
            _students.TryGetValue(id, out var student) ? student : null;

        public IReadOnlyList<Student> GetAll() =>
            _students.Values.ToList().AsReadOnly();

        public void Update(Student student) =>
            _students[student.Id] = student;

        public bool Delete(Guid id) =>
            _students.Remove(id);
    }

    [Fact]
    public async Task Given_validCommand_When_handlingBookSession_Then_returnsSuccessfulResult()
    {
        var sessionRepo = new FakeSessionRepository();
        var tutorRepo = new FakeTutorRepository();
        var studentRepo = new FakeStudentRepository();
        var handler = new BookSessionCommandHandler(sessionRepo, tutorRepo, studentRepo);
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var start = new DateTime(2026, 11, 2, 10, 0, 0);
        var end = new DateTime(2026, 11, 2, 11, 0, 0);
        var tutor = new Tutor("Tutor Name", "tutor@test.com", "Bio", 50, tutorId);
        tutor.AddAvailability(new DateRange(start, end));
        tutorRepo.Add(tutor);
        var student = new Student(studentId, "Student Name", "student@test.com");
        studentRepo.Add(student);
        var command = new BookSessionCommand(tutorId, studentId, start, end);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.TutorId.Should().Be(tutorId);
        result.Value.StudentId.Should().Be(studentId);
        result.Value.StartTime.Should().Be(start);
        result.Value.EndTime.Should().Be(end);
        result.Value.Status.Should().Be(SessionStatus.Booked);
        result.Value.Version.Should().Be(1);
    }

    [Fact]
    public async Task Given_emptyTutorId_When_handlingBookSession_Then_returnsValidationError()
    {
        var sessionRepo = new FakeSessionRepository();
        var tutorRepo = new FakeTutorRepository();
        var studentRepo = new FakeStudentRepository();
        var handler = new BookSessionCommandHandler(sessionRepo, tutorRepo, studentRepo);
        var command = new BookSessionCommand(Guid.Empty, Guid.NewGuid(), new DateTime(2026, 11, 2, 10, 0, 0), new DateTime(2026, 11, 2, 11, 0, 0));

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Session.Validation");
    }

    [Fact]
    public async Task Given_emptyStudentId_When_handlingBookSession_Then_returnsValidationError()
    {
        var sessionRepo = new FakeSessionRepository();
        var tutorRepo = new FakeTutorRepository();
        var studentRepo = new FakeStudentRepository();
        var handler = new BookSessionCommandHandler(sessionRepo, tutorRepo, studentRepo);
        var command = new BookSessionCommand(Guid.NewGuid(), Guid.Empty, new DateTime(2026, 11, 2, 10, 0, 0), new DateTime(2026, 11, 2, 11, 0, 0));

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Session.Validation");
    }

    [Fact]
    public async Task Given_sameTutorAndStudentId_When_handlingBookSession_Then_returnsValidationError()
    {
        var sessionRepo = new FakeSessionRepository();
        var tutorRepo = new FakeTutorRepository();
        var studentRepo = new FakeStudentRepository();
        var handler = new BookSessionCommandHandler(sessionRepo, tutorRepo, studentRepo);
        var id = Guid.NewGuid();
        var start = new DateTime(2026, 11, 2, 10, 0, 0);
        var end = new DateTime(2026, 11, 2, 11, 0, 0);
        var tutor = new Tutor("Tutor", "tutor@test.com", "Bio", 50, id);
        tutor.AddAvailability(new DateRange(start, end));
        tutorRepo.Add(tutor);
        var student = new Student(id, "Student", "student@test.com");
        studentRepo.Add(student);
        var command = new BookSessionCommand(id, id, start, end);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Session.Validation");
    }

    [Fact]
    public async Task Given_invalidDateRange_When_handlingBookSession_Then_returnsValidationError()
    {
        var sessionRepo = new FakeSessionRepository();
        var tutorRepo = new FakeTutorRepository();
        var studentRepo = new FakeStudentRepository();
        var handler = new BookSessionCommandHandler(sessionRepo, tutorRepo, studentRepo);
        var command = new BookSessionCommand(Guid.NewGuid(), Guid.NewGuid(), new DateTime(2026, 11, 2, 11, 0, 0), new DateTime(2026, 11, 2, 10, 0, 0));

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Session.Validation");
    }

    [Fact]
    public async Task Given_nonExistentTutor_When_handlingBookSession_Then_returnsNotFoundError()
    {
        var sessionRepo = new FakeSessionRepository();
        var tutorRepo = new FakeTutorRepository();
        var studentRepo = new FakeStudentRepository();
        var handler = new BookSessionCommandHandler(sessionRepo, tutorRepo, studentRepo);
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var student = new Student(studentId, "Student", "student@test.com");
        studentRepo.Add(student);
        var command = new BookSessionCommand(tutorId, studentId, new DateTime(2026, 11, 2, 10, 0, 0), new DateTime(2026, 11, 2, 11, 0, 0));

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("Tutor.NotFound");
    }

    [Fact]
    public async Task Given_nonExistentStudent_When_handlingBookSession_Then_returnsNotFoundError()
    {
        var sessionRepo = new FakeSessionRepository();
        var tutorRepo = new FakeTutorRepository();
        var studentRepo = new FakeStudentRepository();
        var handler = new BookSessionCommandHandler(sessionRepo, tutorRepo, studentRepo);
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var tutor = new Tutor("Tutor", "tutor@test.com", "Bio", 50, tutorId);
        tutor.AddAvailability(new DateRange(new DateTime(2026, 11, 2, 10, 0, 0), new DateTime(2026, 11, 2, 12, 0, 0)));
        tutorRepo.Add(tutor);
        var command = new BookSessionCommand(tutorId, studentId, new DateTime(2026, 11, 2, 10, 0, 0), new DateTime(2026, 11, 2, 11, 0, 0));

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("Student.NotFound");
    }

    [Fact]
    public async Task Given_intervalOutsideTutorAvailability_When_handlingBookSession_Then_returnsValidationError()
    {
        var sessionRepo = new FakeSessionRepository();
        var tutorRepo = new FakeTutorRepository();
        var studentRepo = new FakeStudentRepository();
        var handler = new BookSessionCommandHandler(sessionRepo, tutorRepo, studentRepo);
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var tutor = new Tutor("Tutor", "tutor@test.com", "Bio", 50, tutorId);
        tutor.AddAvailability(new DateRange(new DateTime(2026, 11, 2, 10, 0, 0), new DateTime(2026, 11, 2, 11, 0, 0)));
        tutorRepo.Add(tutor);
        var student = new Student(studentId, "Student", "student@test.com");
        studentRepo.Add(student);
        var command = new BookSessionCommand(tutorId, studentId, new DateTime(2026, 11, 2, 12, 0, 0), new DateTime(2026, 11, 2, 13, 0, 0));

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Session.Validation");
    }

    [Fact]
    public async Task Given_studentHavingTwoSessionsOnSameDay_When_handlingBookSession_Then_returnsValidationError()
    {
        var sessionRepo = new FakeSessionRepository();
        var tutorRepo = new FakeTutorRepository();
        var studentRepo = new FakeStudentRepository();
        var handler = new BookSessionCommandHandler(sessionRepo, tutorRepo, studentRepo);
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var tutor = new Tutor("Tutor", "tutor@test.com", "Bio", 50, tutorId);
        tutor.AddAvailability(new DateRange(new DateTime(2026, 11, 2, 8, 0, 0), new DateTime(2026, 11, 2, 18, 0, 0)));
        tutorRepo.Add(tutor);
        var student = new Student(studentId, "Student", "student@test.com");
        studentRepo.Add(student);
        var s1 = Session.Create(Guid.NewGuid(), studentId, new DateRange(new DateTime(2026, 11, 2, 9, 0, 0), new DateTime(2026, 11, 2, 10, 0, 0)));
        var s2 = Session.Create(Guid.NewGuid(), studentId, new DateRange(new DateTime(2026, 11, 2, 11, 0, 0), new DateTime(2026, 11, 2, 12, 0, 0)));
        sessionRepo.Add(s1);
        sessionRepo.Add(s2);
        var command = new BookSessionCommand(tutorId, studentId, new DateTime(2026, 11, 2, 14, 0, 0), new DateTime(2026, 11, 2, 15, 0, 0));

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Session.DailyLimit");
        result.Error.Description.Should().Contain("Student already has 2 booked sessions today");
    }

    [Fact]
    public async Task Given_studentHavingOverlappingSession_When_handlingBookSession_Then_returnsConflictError()
    {
        var sessionRepo = new FakeSessionRepository();
        var tutorRepo = new FakeTutorRepository();
        var studentRepo = new FakeStudentRepository();
        var handler = new BookSessionCommandHandler(sessionRepo, tutorRepo, studentRepo);
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var tutor = new Tutor("Tutor", "tutor@test.com", "Bio", 50, tutorId);
        tutor.AddAvailability(new DateRange(new DateTime(2026, 11, 2, 8, 0, 0), new DateTime(2026, 11, 2, 18, 0, 0)));
        tutorRepo.Add(tutor);
        var student = new Student(studentId, "Student", "student@test.com");
        studentRepo.Add(student);
        var existingSession = Session.Create(Guid.NewGuid(), studentId, new DateRange(new DateTime(2026, 11, 2, 10, 0, 0), new DateTime(2026, 11, 2, 11, 30, 0)));
        sessionRepo.Add(existingSession);
        var command = new BookSessionCommand(tutorId, studentId, new DateTime(2026, 11, 2, 11, 0, 0), new DateTime(2026, 11, 2, 12, 0, 0));

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("Session.Conflict");
    }

    [Fact]
    public async Task Given_tutorHavingOverlappingSession_When_handlingBookSession_Then_returnsConflictError()
    {
        var sessionRepo = new FakeSessionRepository();
        var tutorRepo = new FakeTutorRepository();
        var studentRepo = new FakeStudentRepository();
        var handler = new BookSessionCommandHandler(sessionRepo, tutorRepo, studentRepo);
        var tutorId = Guid.NewGuid();
        var student1Id = Guid.NewGuid();
        var student2Id = Guid.NewGuid();
        var tutor = new Tutor("Tutor", "tutor@test.com", "Bio", 50, tutorId);
        tutor.AddAvailability(new DateRange(new DateTime(2026, 11, 2, 8, 0, 0), new DateTime(2026, 11, 2, 18, 0, 0)));
        tutorRepo.Add(tutor);
        var student1 = new Student(student1Id, "Student 1", "student1@test.com");
        var student2 = new Student(student2Id, "Student 2", "student2@test.com");
        studentRepo.Add(student1);
        studentRepo.Add(student2);
        var existingSession = Session.Create(tutorId, student1Id, new DateRange(new DateTime(2026, 11, 2, 10, 0, 0), new DateTime(2026, 11, 2, 11, 0, 0)));
        sessionRepo.Add(existingSession);
        var command = new BookSessionCommand(tutorId, student2Id, new DateTime(2026, 11, 2, 10, 30, 0), new DateTime(2026, 11, 2, 11, 30, 0));

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("Session.Conflict");
    }

    [Fact]
    public async Task Given_studentAlreadyHasSessionInAggregate_When_handlingBookSession_Then_doesNotAddDuplicateSessionToAggregate()
    {
        var sessionRepo = new FakeSessionRepository();
        var tutorRepo = new FakeTutorRepository();
        var studentRepo = new FakeStudentRepository();
        var handler = new BookSessionCommandHandler(sessionRepo, tutorRepo, studentRepo);
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var tutor = new Tutor("Tutor", "tutor@test.com", "Bio", 50, tutorId);
        tutor.AddAvailability(new DateRange(new DateTime(2026, 11, 2, 8, 0, 0), new DateTime(2026, 11, 2, 18, 0, 0)));
        tutorRepo.Add(tutor);
        var student = new Student(studentId, "Student", "student@test.com");
        var pastSession = Session.Create(Guid.NewGuid(), studentId, new DateRange(new DateTime(2026, 11, 1, 10, 0, 0), new DateTime(2026, 11, 1, 11, 0, 0)));
        student.AddSession(pastSession);
        sessionRepo.Add(pastSession);
        studentRepo.Add(student);
        var command = new BookSessionCommand(tutorId, studentId, new DateTime(2026, 11, 2, 10, 0, 0), new DateTime(2026, 11, 2, 11, 0, 0));

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        student.Sessions.Should().HaveCount(2);
    }

    [Fact]
    public async Task Given_dbUpdateConcurrencyExceptionOnAdd_When_handlingBookSession_Then_returnsConflictError()
    {
        var sessionRepo = new FakeSessionRepository
        {
            AddBehavior = _ => throw new DbUpdateConcurrencyException("Concurrency conflict")
        };
        var tutorRepo = new FakeTutorRepository();
        var studentRepo = new FakeStudentRepository();
        var handler = new BookSessionCommandHandler(sessionRepo, tutorRepo, studentRepo);
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var tutor = new Tutor("Tutor", "tutor@test.com", "Bio", 50, tutorId);
        tutor.AddAvailability(new DateRange(new DateTime(2026, 11, 2, 10, 0, 0), new DateTime(2026, 11, 2, 12, 0, 0)));
        tutorRepo.Add(tutor);
        var student = new Student(studentId, "Student", "student@test.com");
        studentRepo.Add(student);
        var command = new BookSessionCommand(tutorId, studentId, new DateTime(2026, 11, 2, 10, 0, 0), new DateTime(2026, 11, 2, 11, 0, 0));

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("Session.Conflict");
    }

    [Fact]
    public async Task Given_invalidOperationExceptionOnAdd_When_handlingBookSession_Then_returnsConflictError()
    {
        var sessionRepo = new FakeSessionRepository
        {
            AddBehavior = _ => throw new InvalidOperationException("Duplicate session key")
        };
        var tutorRepo = new FakeTutorRepository();
        var studentRepo = new FakeStudentRepository();
        var handler = new BookSessionCommandHandler(sessionRepo, tutorRepo, studentRepo);
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var tutor = new Tutor("Tutor", "tutor@test.com", "Bio", 50, tutorId);
        tutor.AddAvailability(new DateRange(new DateTime(2026, 11, 2, 10, 0, 0), new DateTime(2026, 11, 2, 12, 0, 0)));
        tutorRepo.Add(tutor);
        var student = new Student(studentId, "Student", "student@test.com");
        studentRepo.Add(student);
        var command = new BookSessionCommand(tutorId, studentId, new DateTime(2026, 11, 2, 10, 0, 0), new DateTime(2026, 11, 2, 11, 0, 0));

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("Session.Conflict");
    }
}
