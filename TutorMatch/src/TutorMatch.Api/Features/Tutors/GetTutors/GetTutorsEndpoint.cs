using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Tutors.GetTutors;

public static class GetTutorsEndpoint
{
    public static IEndpointRouteBuilder MapGetTutors(this IEndpointRouteBuilder group)
    {
        group.MapGet("/", HandleAsync);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        IQueryHandler<GetTutorsQuery, Result<List<TutorSummaryResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetTutorsQuery();
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.ToHttpResult();
    }
}
