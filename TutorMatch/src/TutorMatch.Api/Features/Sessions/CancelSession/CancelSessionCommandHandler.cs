using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Features.Sessions.CancelSession;

public class CancelSessionCommandHandler : ICommandHandler<CancelSessionCommand>
{
    private readonly ISessionRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CancelSessionCommandHandler(ISessionRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public Task<Result> HandleAsync(CancelSessionCommand command, CancellationToken cancellationToken = default)
    {
        if (command.SessionId == Guid.Empty)
        {
            return Task.FromResult(Result.Failure(
                Error.Validation("Session.Validation", "SessionId cannot be empty.")));
        }

        if (command.RequesterId == Guid.Empty)
        {
            return Task.FromResult(Result.Failure(
                Error.Validation("Session.Validation", "RequesterId cannot be empty.")));
        }

        var session = _repository.GetById(command.SessionId);
        if (session is null)
        {
            return Task.FromResult(Result.Failure(
                Error.NotFound("Session.NotFound", $"Session with ID '{command.SessionId}' was not found.")));
        }

        if (command.RequesterId != session.StudentId && command.RequesterId != session.TutorId)
        {
            return Task.FromResult(Result.Failure(
                Error.Conflict("Session.Conflict", "Only the student or tutor involved in the session can cancel it.")));
        }

        try
        {
            session.Cancel(_timeProvider);
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

