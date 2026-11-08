using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Features.Sessions.GetMySessions;

public class GetMySessionsQueryHandler : IQueryHandler<GetMySessionsQuery, Result<GetMySessionsResponse>>
{
    private readonly ISessionRepository _repository;

    public GetMySessionsQueryHandler(ISessionRepository repository)
    {
        _repository = repository;
    }

    public Task<Result<GetMySessionsResponse>> HandleAsync(GetMySessionsQuery query, CancellationToken cancellationToken = default)
    {
        if (query.UserId == Guid.Empty)
        {
            return Task.FromResult(Result<GetMySessionsResponse>.Failure(
                Error.Validation("Session.Validation", "UserId cannot be empty.")));
        }

        if (string.IsNullOrWhiteSpace(query.Role) ||
            (!query.Role.Equals("Student", StringComparison.OrdinalIgnoreCase) &&
             !query.Role.Equals("Tutor", StringComparison.OrdinalIgnoreCase)))
        {
            return Task.FromResult(Result<GetMySessionsResponse>.Failure(
                Error.Validation("Session.Validation", "Role must be either 'Student' or 'Tutor'.")));
        }

        var isStudent = query.Role.Equals("Student", StringComparison.OrdinalIgnoreCase);

        var sessions = _repository.GetAll()
            .Where(s => isStudent ? s.StudentId == query.UserId : s.TutorId == query.UserId);

        if (query.Status.HasValue)
        {
            sessions = sessions.Where(s => s.Status == query.Status.Value);
        }

        var mappedSessions = sessions
            .OrderBy(s => s.StartTime)
            .Select(s => new MySessionDto(
                s.Id,
                s.TutorId,
                s.StudentId,
                s.StartTime,
                s.EndTime,
                s.Status,
                s.Version,
                s.Subject,
                s.Notes))
            .ToList();

        var response = new GetMySessionsResponse(mappedSessions);
        return Task.FromResult(Result<GetMySessionsResponse>.Success(response));
    }
}
