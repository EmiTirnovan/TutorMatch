using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Features.Sessions.GetSessionById;

public class GetSessionByIdQueryHandler : IQueryHandler<GetSessionByIdQuery, Result<GetSessionByIdResponse>>
{
    private readonly ISessionRepository _repository;

    public GetSessionByIdQueryHandler(ISessionRepository repository)
    {
        _repository = repository;
    }

    public Task<Result<GetSessionByIdResponse>> HandleAsync(GetSessionByIdQuery query, CancellationToken cancellationToken = default)
    {
        var session = _repository.GetById(query.Id);
        if (session is null)
        {
            return Task.FromResult(Result<GetSessionByIdResponse>.Failure(
                Error.NotFound("Session.NotFound", $"Session with ID '{query.Id}' was not found.")));
        }

        var response = new GetSessionByIdResponse(
            session.Id,
            session.TutorId,
            session.StudentId,
            session.StartTime,
            session.EndTime,
            session.Status,
            session.Version);

        return Task.FromResult(Result<GetSessionByIdResponse>.Success(response));
    }
}
