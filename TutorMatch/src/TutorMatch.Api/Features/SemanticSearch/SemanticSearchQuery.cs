using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.SemanticSearch;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record SemanticSearchQuery(string Query) : IQuery<Result<SemanticSearchResponse>>;

