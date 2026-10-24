using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Features.Sessions.UpdateSession;

public class UpdateSessionCommandHandler : ICommandHandler<UpdateSessionCommand>
{
    private readonly ISessionRepository _repository;

    public UpdateSessionCommandHandler(ISessionRepository repository)
    {
        _repository = repository;
    }

    public Task<Result> HandleAsync(UpdateSessionCommand command, CancellationToken cancellationToken = default)
    {
        var session = _repository.GetById(command.Id);
        if (session is null)
        {
            return Task.FromResult(Result.Failure(
                Error.NotFound("Session.NotFound", $"Session with ID '{command.Id}' was not found.")));
        }

        try
        {
            session.Reschedule(new DateRange(command.StartTime, command.EndTime));
            _repository.Update(session);

            return Task.FromResult(Result.Success());
        }
        catch (ArgumentException ex)
        {
            return Task.FromResult(Result.Failure(
                Error.Validation("Session.Validation", ex.Message)));
        }
        catch (InvalidOperationException ex)
        {
            return Task.FromResult(Result.Failure(
                Error.Conflict("Session.Conflict", ex.Message)));
        }
    }
}
