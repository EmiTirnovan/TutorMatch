using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Students;

namespace TutorMatch.Api.Features.Students.UpdateProfile;

public class UpdateStudentProfileCommandHandler : ICommandHandler<UpdateStudentProfileCommand>
{
    private readonly IStudentRepository _repository;

    public UpdateStudentProfileCommandHandler(IStudentRepository repository)
    {
        _repository = repository;
    }

    public Task<Result> HandleAsync(UpdateStudentProfileCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Id == Guid.Empty)
        {
            return Task.FromResult(Result.Failure(
                Error.Validation("Student.Validation", "Student ID cannot be empty.")));
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Task.FromResult(Result.Failure(
                Error.Validation("Student.Validation", "Student name cannot be empty.")));
        }

        var student = _repository.GetById(command.Id);
        if (student is null)
        {
            return Task.FromResult(Result.Failure(
                Error.NotFound("Student.NotFound", $"Student with ID '{command.Id}' was not found.")));
        }

        try
        {
            student.UpdateProfile(command.Name);
            _repository.Update(student);
            return Task.FromResult(Result.Success());
        }
        catch (ArgumentException ex)
        {
            return Task.FromResult(Result.Failure(
                Error.Validation("Student.Validation", ex.Message)));
        }
    }
}
