namespace TutorMatch.Api.Features.Discovery.SearchTutors;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record SearchTutorsRequest(
    string? Subject = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    DateTime? Date = null,
    int Page = 1,
    int PageSize = 10,
    string? SortBy = null);