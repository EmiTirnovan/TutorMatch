using Microsoft.EntityFrameworkCore;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Infrastructure.Persistence;

namespace TutorMatch.Api.Features.Subjects.SuggestSubjects;

public class GetSubjectSuggestionsQueryHandler : IQueryHandler<GetSubjectSuggestionsQuery, Result<SubjectSuggestionResponse>>
{
    private readonly TutorMatchDbContext _dbContext;

    public GetSubjectSuggestionsQueryHandler(TutorMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<SubjectSuggestionResponse>> HandleAsync(
        GetSubjectSuggestionsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Query))
        {
            return Result<SubjectSuggestionResponse>.Success(
                new SubjectSuggestionResponse(new List<string>()));
        }

        var trimmed = query.Query.Trim();

        var suggestions = await _dbContext.Subjects
            .AsNoTracking()
            .Where(s => EF.Functions.Like(s.Name, $"%{trimmed}%"))
            .OrderByDescending(s => s.UsageCount)
            .ThenBy(s => s.Name)
            .Take(3)
            .Select(s => s.Name)
            .ToListAsync(cancellationToken);

        return Result<SubjectSuggestionResponse>.Success(new SubjectSuggestionResponse(suggestions));
    }
}
