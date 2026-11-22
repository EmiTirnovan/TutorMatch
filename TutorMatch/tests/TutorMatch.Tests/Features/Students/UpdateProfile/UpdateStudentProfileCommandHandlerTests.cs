using FluentAssertions;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Students;
using TutorMatch.Api.Features.Students.UpdateProfile;
using Xunit;

namespace TutorMatch.Tests.Features.Students.UpdateProfile;

public class UpdateStudentProfileCommandHandlerTests
{
    private sealed class FakeStudentRepo : IStudentRepository
    {
        private readonly Dictionary<Guid, Student> _students = new();

        public void Add(Student student) => _students[student.Id] = student;
        public Student? GetById(Guid id) => _students.TryGetValue(id, out var s) ? s : null;
        public IReadOnlyList<Student> GetAll() => _students.Values.ToList().AsReadOnly();
        public void Update(Student student) => _students[student.Id] = student;
        public bool Delete(Guid id) => _students.Remove(id);
    }

    [Fact]
    public async Task Given_EmptyStudentId_When_UpdatingProfile_Then_ReturnsValidationError()
    {
        var repo = new FakeStudentRepo();
        var handler = new UpdateStudentProfileCommandHandler(repo);

        var result = await handler.HandleAsync(new UpdateStudentProfileCommand(Guid.Empty, "Alice"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Given_EmptyName_When_UpdatingProfile_Then_ReturnsValidationError(string emptyName)
    {
        var repo = new FakeStudentRepo();
        var handler = new UpdateStudentProfileCommandHandler(repo);
        var studentId = Guid.NewGuid();
        repo.Add(new Student(studentId, "Initial Name", "student@test.com"));

        var result = await handler.HandleAsync(new UpdateStudentProfileCommand(studentId, emptyName));

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Given_NonexistentStudent_When_UpdatingProfile_Then_ReturnsNotFoundError()
    {
        var repo = new FakeStudentRepo();
        var handler = new UpdateStudentProfileCommandHandler(repo);

        var result = await handler.HandleAsync(new UpdateStudentProfileCommand(Guid.NewGuid(), "New Name"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Given_ValidCommand_When_UpdatingProfile_Then_UpdatesStudentNameAndPersists()
    {
        var repo = new FakeStudentRepo();
        var handler = new UpdateStudentProfileCommandHandler(repo);
        var studentId = Guid.NewGuid();
        var student = new Student(studentId, "Old Name", "student@test.com");
        repo.Add(student);

        var result = await handler.HandleAsync(new UpdateStudentProfileCommand(studentId, "Updated Student Name"));

        result.IsSuccess.Should().BeTrue();
        var updated = repo.GetById(studentId)!;
        updated.Name.Should().Be("Updated Student Name");
        updated.Email.Should().Be("student@test.com"); // Email unchanged
    }
}
