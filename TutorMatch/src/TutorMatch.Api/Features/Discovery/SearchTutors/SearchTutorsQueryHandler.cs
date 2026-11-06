using Microsoft.EntityFrameworkCore;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Infrastructure.Persistence;

namespace TutorMatch.Api.Features.Discovery.SearchTutors;

public class SearchTutorsQueryHandler : IQueryHandler<SearchTutorsQuery, Result<SearchTutorsResponse>>
{
    private readonly TutorMatchDbContext _dbContext;

    public SearchTutorsQueryHandler(TutorMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<SearchTutorsResponse>> HandleAsync(SearchTutorsQuery query, CancellationToken cancellationToken = default)
    {
        var queryable = _dbContext.Tutors.AsQueryable();

        if (query.MinPrice.HasValue)
        {
            queryable = queryable.Where(t => t.HourlyRate >= query.MinPrice.Value);
        }

        if (query.MaxPrice.HasValue)
        {
            queryable = queryable.Where(t => t.HourlyRate <= query.MaxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Subject))
        {
            queryable = queryable.Where(t => t.TutorSubjects
                .Any(ts => ts.Subject.Name.Contains(query.Subject)));
        }

        if (query.Date.HasValue)
        {
            var targetDate = query.Date.Value.Date;
            queryable = queryable.Where(t => t.Availability
                .Any(a => a.Start.Date <= targetDate && a.End.Date >= targetDate));
        }

        var totalCount = await queryable.CountAsync(cancellationToken);

        queryable = query.SortBy?.ToLower() switch
        {
            "pricedesc" => queryable.OrderByDescending(t => (double)t.HourlyRate),
            "priceasc" => queryable.OrderBy(t => (double)t.HourlyRate),
            _ => queryable.OrderBy(t => (double)t.HourlyRate)
        };

        var page = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.HourlyRate,
                AverageRating = _dbContext.Ratings
                    .Where(r => r.TutorId == t.Id)
                    .Average(r => (double?)r.Score),
                RatingCount = _dbContext.Ratings.Count(r => r.TutorId == t.Id),
                SubjectNames = t.TutorSubjects.Select(ts => ts.Subject.Name).ToList()
            })
            .ToListAsync(cancellationToken);

        var tutors = page
            .Select(t => new TutorSearchDto(
                t.Id,
                t.Name,
                t.HourlyRate,
                t.AverageRating ?? 0,
                t.RatingCount,
                t.SubjectNames.Order(StringComparer.OrdinalIgnoreCase).ToList()))
            .ToList();

        var response = new SearchTutorsResponse(tutors, totalCount, query.Page, query.PageSize);
        return Result<SearchTutorsResponse>.Success(response);
    }
}