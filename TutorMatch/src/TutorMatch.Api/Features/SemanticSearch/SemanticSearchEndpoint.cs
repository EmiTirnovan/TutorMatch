using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.SemanticSearch;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class SemanticSearchEndpoint
{
    public static IEndpointRouteBuilder MapSemanticSearchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tutors/semantic-search", HandleAsync)
           .WithTags("Tutors");

        return app;
    }

    public static IEndpointRouteBuilder MapSemanticSearch(this IEndpointRouteBuilder group)
    {
        group.MapGet("/semantic-search", HandleAsync)
             .WithTags("Tutors");

        return group;
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery(Name = "q")] string? q,
        [FromServices] IQueryHandler<SemanticSearchQuery, Result<SemanticSearchResponse>> handler,
        CancellationToken cancellationToken)
    {
        var query = new SemanticSearchQuery(q ?? string.Empty);
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.ToHttpResult();
    }
}
