using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Features.Sessions.CreateSession;

public class CreateSessionCommandHandler : ICommandHandler<CreateSessionCommand, Result<CreateSessionResponse>>
{
    private readonly ISessionRepository _repository;

    public CreateSessionCommandHandler(ISessionRepository repository)
    {
        _repository = repository;
    }

    public Task<Result<CreateSessionResponse>> HandleAsync(CreateSessionCommand command, CancellationToken cancellationToken = default)
    {
        if (command.TutorId == Guid.Empty || command.StudentId == Guid.Empty)
        {
            return Task.FromResult(Result<CreateSessionResponse>.Failure(
                Error.Validation("Session.Validation", "TutorId and StudentId must be valid GUIDs.")));
        }

        try
        {
            var interval = new DateRange(command.StartTime, command.EndTime);
            var session = Session.Create(command.TutorId, command.StudentId, interval);

            _repository.Add(session);

            var response = new CreateSessionResponse(
                session.Id,
                session.TutorId,
                session.StudentId,
                session.StartTime,
                session.EndTime,
                session.Status,
                session.Version);

            return Task.FromResult(Result<CreateSessionResponse>.Success(response));
        }
        catch (ArgumentException ex)
        {
            return Task.FromResult(Result<CreateSessionResponse>.Failure(
                Error.Validation("Session.Validation", ex.Message)));
        }
        catch (InvalidOperationException ex)
        {
            return Task.FromResult(Result<CreateSessionResponse>.Failure(
                Error.Conflict("Session.Conflict", ex.Message)));
        }
    }
}
