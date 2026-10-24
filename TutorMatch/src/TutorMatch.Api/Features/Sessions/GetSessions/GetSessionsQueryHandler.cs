using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Features.Sessions.GetSessions;

public class GetSessionsQueryHandler : IQueryHandler<GetSessionsQuery, Result<List<GetSessionsResponse>>>
{
    private readonly ISessionRepository _repository;

    public GetSessionsQueryHandler(ISessionRepository repository)
    {
        _repository = repository;
    }

    public Task<Result<List<GetSessionsResponse>>> HandleAsync(GetSessionsQuery query, CancellationToken cancellationToken = default)
    {
        var sessions = _repository.GetAll()
            .Select(session => new GetSessionsResponse(
                session.Id,
                session.TutorId,
                session.StudentId,
                session.StartTime,
                session.EndTime,
                session.Status,
                session.Version))
            .ToList();

        return Task.FromResult(Result<List<GetSessionsResponse>>.Success(sessions));
    }
}
