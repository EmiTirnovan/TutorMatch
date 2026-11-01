using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Features.Sessions.CompleteSession;

public class CompleteSessionCommandHandler : ICommandHandler<CompleteSessionCommand>
{
    private readonly ISessionRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CompleteSessionCommandHandler(ISessionRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public Task<Result> HandleAsync(CompleteSessionCommand command, CancellationToken cancellationToken = default)
    {
        if (command.SessionId == Guid.Empty)
        {
            return Task.FromResult(Result.Failure(
                Error.Validation("Session.Validation", "SessionId cannot be empty.")));
        }

        if (command.TutorId == Guid.Empty)
        {
            return Task.FromResult(Result.Failure(
                Error.Validation("Session.Validation", "TutorId cannot be empty.")));
        }

        var session = _repository.GetById(command.SessionId);
        if (session is null)
        {
            return Task.FromResult(Result.Failure(
                Error.NotFound("Session.NotFound", $"Session with ID '{command.SessionId}' was not found.")));
        }

        if (command.TutorId != session.TutorId)
        {
            return Task.FromResult(Result.Failure(
                Error.Conflict("Session.Conflict", "Only the assigned tutor can mark this session as completed.")));
        }

        try
        {
            session.Complete(_timeProvider);
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

