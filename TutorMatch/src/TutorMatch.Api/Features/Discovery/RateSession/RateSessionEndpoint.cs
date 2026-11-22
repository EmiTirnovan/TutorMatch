using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Discovery.RateSession;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class RateSessionEndpoint
{
    public static IEndpointRouteBuilder MapRateSession(this IEndpointRouteBuilder group)
    {
        group.MapPost("/{tutorId:guid}/sessions/{sessionId:guid}/rate", HandleAsync)
            .RequireAuthorization()
            .WithSummary("Rate a completed session")
            .WithDescription("The student is identified by the X-Student-Id header.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict)
            .WithOpenApi(operation =>
            {
                operation.Parameters.First(p => p.Name == "X-Student-Id").Description = "Id of the student making the request (no auth yet).";
                return operation;
            });
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid tutorId,
        Guid sessionId,
        [FromHeader(Name = "X-Student-Id")] Guid studentId,
        [FromBody] RateSessionRequest request,
        [FromServices] ICommandHandler<RateSessionCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new RateSessionCommand(
            sessionId,
            studentId,
            request.Score,
            request.Notes);

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToHttpResult();
    }
}