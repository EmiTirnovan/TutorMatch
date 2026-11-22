using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Discovery.SearchTutors;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class SearchTutorsEndpoint
{
    public static IEndpointRouteBuilder MapSearchTutors(this IEndpointRouteBuilder group)
    {
        group.MapGet("/search", HandleAsync)
            .WithSummary("Search tutors with filters")
            .WithDescription("Filters by subject, price range, minimum average rating and available date, with paging. SortBy accepts priceAsc (default), priceDesc and rating_desc.")
            .Produces<SearchTutorsResponse>(StatusCodes.Status200OK);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        [AsParameters] SearchTutorsRequest request,
        [FromServices] IQueryHandler<SearchTutorsQuery, Result<SearchTutorsResponse>> handler,
        CancellationToken cancellationToken)
    {
        var query = new SearchTutorsQuery(
            request.Subject,
            request.MinPrice,
            request.MaxPrice,
            request.Date,
            request.Page <= 0 ? 1 : request.Page,
            request.PageSize <= 0 ? 10 : request.PageSize,
            request.SortBy,
            request.MinRating);

        var result = await handler.HandleAsync(query, cancellationToken);
        return result.ToHttpResult();
    }
}