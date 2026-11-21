using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Domain.Students;
using TutorMatch.Api.Domain.Tutors;

namespace TutorMatch.Api.Features.Sessions.GetMySessions;

public class GetMySessionsQueryHandler : IQueryHandler<GetMySessionsQuery, Result<GetMySessionsResponse>>
{
    private readonly ISessionRepository _repository;
    private readonly ITutorRepository? _tutorRepository;
    private readonly IStudentRepository? _studentRepository;
    private readonly IRatingRepository? _ratingRepository;

    public GetMySessionsQueryHandler(
        ISessionRepository repository,
        ITutorRepository? tutorRepository = null,
        IStudentRepository? studentRepository = null,
        IRatingRepository? ratingRepository = null)
    {
        _repository = repository;
        _tutorRepository = tutorRepository;
        _studentRepository = studentRepository;
        _ratingRepository = ratingRepository;
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

        var orderedSessions = sessions.OrderBy(s => s.StartTime).ToList();
        var ratingsBySession = LoadRatingsBySession(orderedSessions);

        var mappedSessions = orderedSessions
            .Select(s =>
            {
                ratingsBySession.TryGetValue(s.Id, out var rating);
                var tutorName = _tutorRepository?.GetById(s.TutorId)?.Name;
                var studentName = _studentRepository?.GetById(s.StudentId)?.Name;
                return new MySessionDto(
                    s.Id,
                    s.TutorId,
                    s.StudentId,
                    s.StartTime,
                    s.EndTime,
                    s.Status,
                    s.Version,
                    s.Subject,
                    s.Notes,
                    tutorName,
                    studentName,
                    s.Type,
                    s.MeetingLink,
                    s.LocationName,
                    s.Latitude,
                    s.Longitude,
                    rating is not null,
                    rating?.Score,
                    rating?.Notes);
            })
            .ToList();

        var response = new GetMySessionsResponse(mappedSessions);
        return Task.FromResult(Result<GetMySessionsResponse>.Success(response));
    }

    private Dictionary<Guid, Rating> LoadRatingsBySession(IReadOnlyCollection<Session> sessions)
    {
        if (_ratingRepository is null || sessions.Count == 0)
        {
            return new Dictionary<Guid, Rating>();
        }

        return _ratingRepository.GetBySessionIds(sessions.Select(s => s.Id))
            .GroupBy(r => r.SessionId)
            .ToDictionary(g => g.Key, g => g.First());
    }
}
