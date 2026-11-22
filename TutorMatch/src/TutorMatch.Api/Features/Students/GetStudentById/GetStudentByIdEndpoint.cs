using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Students.GetStudentById;

// Minimal API endpoint wiring is not unit-testable and is excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class GetStudentByIdEndpoint
{
    public static IEndpointRouteBuilder MapGetStudentById(this IEndpointRouteBuilder group)
    {
        group.MapGet("/{id:guid}", HandleAsync)
            .WithSummary("Get student profile by ID")
            .WithDescription("Retrieves the student's profile information.")
            .Produces<GetStudentByIdResponse>(StatusCodes.Status200OK)
            .ProducesErrors(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        IQueryHandler<GetStudentByIdQuery, Result<GetStudentByIdResponse>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetStudentByIdQuery(id);
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.ToHttpResult();
    }
}
