using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Features.Sessions.DeleteSession;

public class DeleteSessionCommandHandler : ICommandHandler<DeleteSessionCommand>
{
    private readonly ISessionRepository _repository;

    public DeleteSessionCommandHandler(ISessionRepository repository)
    {
        _repository = repository;
    }

    public Task<Result> HandleAsync(DeleteSessionCommand command, CancellationToken cancellationToken = default)
    {
        var deleted = _repository.Delete(command.Id);
        if (!deleted)
        {
            return Task.FromResult(Result.Failure(
                Error.NotFound("Session.NotFound", $"Session with ID '{command.Id}' was not found.")));
        }

        return Task.FromResult(Result.Success());
    }
}
