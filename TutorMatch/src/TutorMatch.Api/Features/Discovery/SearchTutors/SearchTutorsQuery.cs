using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Discovery.SearchTutors;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record SearchTutorsQuery(
    string? Subject,
    decimal? MinPrice,
    decimal? MaxPrice,
    DateTime? Date,
    int Page,
    int PageSize,
    string? SortBy) : IQuery<Result<SearchTutorsResponse>>;