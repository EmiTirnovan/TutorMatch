using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Discovery.SearchTutors;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record SearchTutorsQuery(
    string? Subject = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    DateTime? Date = null,
    int Page = 1,
    int PageSize = 10,
    string? SortBy = null) : IQuery<Result<SearchTutorsResponse>>
{
    public SearchTutorsQuery(string? subject) : this(subject, null, null, null, 1, 10, null)
    {
    }
}
