using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Subjects.SuggestSubjects;

// Minimal API endpoint wiring is not unit-testable and is excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class SuggestSubjectsEndpoint
{
    public static IEndpointRouteBuilder MapSuggestSubjects(this IEndpointRouteBuilder group)
    {
        group.MapGet("/suggest", HandleAsync)
            .WithSummary("Get subject suggestions for autocomplete")
            .WithDescription("Returns up to three subject suggestions matching query string q, ordered by usage count descending and name alphabetically.")
            .Produces<SubjectSuggestionResponse>(StatusCodes.Status200OK);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] string? q,
        IQueryHandler<GetSubjectSuggestionsQuery, Result<SubjectSuggestionResponse>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetSubjectSuggestionsQuery(q);
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.ToHttpResult();
    }
}
