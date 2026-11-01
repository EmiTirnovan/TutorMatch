namespace TutorMatch.Api.Features.SemanticSearch;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record SemanticSearchResponse(
    IReadOnlyList<SemanticSearchResultItem> Results,
    string Mode,
    string? Message = null);

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record SemanticSearchResultItem(
    Guid TutorId,
    string Name,
    string Bio,
    decimal HourlyRate,
    float SimilarityScore,
    IReadOnlyList<string> Subjects);

