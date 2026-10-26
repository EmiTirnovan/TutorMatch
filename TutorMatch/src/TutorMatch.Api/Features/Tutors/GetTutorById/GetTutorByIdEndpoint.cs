using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Tutors.GetTutorById;

// Minimal API endpoint wiring is not unit-testable and is excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class GetTutorByIdEndpoint
{
    public static IEndpointRouteBuilder MapGetTutorById(this IEndpointRouteBuilder group)
    {
        group.MapGet("/{id:guid}", HandleAsync);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        IQueryHandler<GetTutorByIdQuery, Result<GetTutorByIdResponse>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetTutorByIdQuery(id);
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.ToHttpResult();
    }
}
