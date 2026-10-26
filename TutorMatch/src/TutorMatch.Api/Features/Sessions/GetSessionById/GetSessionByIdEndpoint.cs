using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Sessions.GetSessionById;

// Minimal API endpoint wiring is not unit-testable and is excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class GetSessionByIdEndpoint
{
    public static IEndpointRouteBuilder MapGetSessionById(this IEndpointRouteBuilder group)
    {
        group.MapGet("/{id:guid}", HandleAsync);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        IQueryHandler<GetSessionByIdQuery, Result<GetSessionByIdResponse>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetSessionByIdQuery(id);
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.ToHttpResult();
    }
}
