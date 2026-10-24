using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Tutors;

namespace TutorMatch.Api.Features.Tutors.DeleteTutor;

public class DeleteTutorCommandHandler : ICommandHandler<DeleteTutorCommand>
{
    private readonly ITutorRepository _repository;

    public DeleteTutorCommandHandler(ITutorRepository repository)
    {
        _repository = repository;
    }

    public Task<Result> HandleAsync(DeleteTutorCommand command, CancellationToken cancellationToken = default)
    {
        var deleted = _repository.Delete(command.Id);
        if (!deleted)
        {
            return Task.FromResult(Result.Failure(
                Error.NotFound("Tutor.NotFound", $"Tutor with ID '{command.Id}' was not found.")));
        }

        return Task.FromResult(Result.Success());
    }
}
