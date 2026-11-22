using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Dtos;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Domain.Students;
using TutorMatch.Api.Domain.Tutors;

namespace TutorMatch.Api.Features.Tutors.GetTutorById;

public class GetTutorByIdQueryHandler : IQueryHandler<GetTutorByIdQuery, Result<GetTutorByIdResponse>>
{
    public const int MaxRecentReviews = 20;

    private readonly ITutorRepository _repository;
    private readonly IRatingRepository? _ratingRepository;
    private readonly ISessionRepository? _sessionRepository;
    private readonly IStudentRepository? _studentRepository;

    public GetTutorByIdQueryHandler(
        ITutorRepository repository,
        IRatingRepository? ratingRepository = null,
        ISessionRepository? sessionRepository = null,
        IStudentRepository? studentRepository = null)
    {
        _repository = repository;
        _ratingRepository = ratingRepository;
        _sessionRepository = sessionRepository;
        _studentRepository = studentRepository;
    }

    public Task<Result<GetTutorByIdResponse>> HandleAsync(GetTutorByIdQuery query, CancellationToken cancellationToken = default)
    {
        var tutor = _repository.GetById(query.Id);
        if (tutor is null)
        {
            return Task.FromResult(Result<GetTutorByIdResponse>.Failure(
                Error.NotFound("Tutor.NotFound", $"Tutor with ID '{query.Id}' was not found.")));
        }

        var subjects = tutor.TutorSubjects
            .Select(ts => ts.Subject?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToList();

        var ratings = _ratingRepository?.GetByTutorId(tutor.Id) ?? Array.Empty<Rating>();

        var response = new GetTutorByIdResponse(
            tutor.Id,
            tutor.Name,
            tutor.Email,
            tutor.Bio,
            tutor.HourlyRate,
            tutor.TutorSubjects.Select(ts => ts.SubjectId).ToList(),
            tutor.Availability.Select(a => new DateRangeDto(a.Start, a.End)).ToList(),
            subjects,
            ratings.Count == 0 ? 0 : ratings.Average(r => r.Score),
            ratings.Count,
            BuildRecentReviews(ratings),
            BuildDistribution(ratings)
        );

        return Task.FromResult(Result<GetTutorByIdResponse>.Success(response));
    }

    private List<TutorReviewDto> BuildRecentReviews(IReadOnlyList<Rating> ratings)
    {
        return ratings
            .Select(rating =>
            {
                var session = _sessionRepository?.GetById(rating.SessionId);
                var studentName = _studentRepository?.GetById(rating.StudentId)?.Name;
                return new TutorReviewDto(
                    rating.SessionId,
                    session?.StartTime ?? rating.CreatedAt,
                    rating.Score,
                    rating.Notes,
                    studentName);
            })
            .OrderByDescending(review => review.Date)
            .Take(MaxRecentReviews)
            .ToList();
    }

    // Always contains the keys 1..5 so the UI can render every bar, even when a star level has no votes.
    private static Dictionary<int, int> BuildDistribution(IReadOnlyList<Rating> ratings)
    {
        var distribution = Enumerable.Range(1, 5).ToDictionary(star => star, _ => 0);
        foreach (var rating in ratings)
        {
            distribution[rating.Score]++;
        }

        return distribution;
    }
}
