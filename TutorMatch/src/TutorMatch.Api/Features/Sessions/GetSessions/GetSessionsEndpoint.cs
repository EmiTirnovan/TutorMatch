using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Sessions.GetSessions;

public static class GetSessionsEndpoint
{
    public static IEndpointRouteBuilder MapGetSessions(this IEndpointRouteBuilder group)
    {
        group.MapGet("/", HandleAsync);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        IQueryHandler<GetSessionsQuery, Result<List<GetSessionsResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetSessionsQuery();
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.ToHttpResult();
    }
}
