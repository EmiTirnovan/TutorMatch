using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Tutors;

namespace TutorMatch.Api.Features.Tutors.UpdateTutor;

public class UpdateTutorCommandHandler : ICommandHandler<UpdateTutorCommand>
{
    private readonly ITutorRepository _repository;

    public UpdateTutorCommandHandler(ITutorRepository repository)
    {
        _repository = repository;
    }

    public Task<Result> HandleAsync(UpdateTutorCommand command, CancellationToken cancellationToken = default)
    {
        var tutor = _repository.GetById(command.Id);
        if (tutor is null)
        {
            return Task.FromResult(Result.Failure(
                Error.NotFound("Tutor.NotFound", $"Tutor with ID '{command.Id}' was not found.")));
        }

        try
        {
            tutor.UpdateProfile(command.Name, command.Bio, command.HourlyRate);
            _repository.Update(tutor);

            return Task.FromResult(Result.Success());
        }
        catch (ArgumentException ex)
        {
            return Task.FromResult(Result.Failure(
                Error.Validation("Tutor.Validation", ex.Message)));
        }
        catch (InvalidOperationException ex)
        {
            return Task.FromResult(Result.Failure(
                Error.Conflict("Tutor.Conflict", ex.Message)));
        }
    }
}
