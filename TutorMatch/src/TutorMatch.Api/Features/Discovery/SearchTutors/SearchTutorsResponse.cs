namespace TutorMatch.Api.Features.Discovery.SearchTutors;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record SearchTutorsResponse(
    IEnumerable<TutorSearchDto> Tutors,
    int TotalCount,
    int Page,
    int PageSize,
    string Mode = "keyword");

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record TutorSearchDto(
    Guid Id,
    string Name,
    decimal HourlyRate,
    double AverageRating,
    int RatingCount);