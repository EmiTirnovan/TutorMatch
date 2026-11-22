using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Subjects.SuggestSubjects;

// DTOs and messages contain no business logic and are excluded from unit testing.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record GetSubjectSuggestionsQuery(string? Query) : IQuery<Result<SubjectSuggestionResponse>>;
