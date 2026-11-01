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
        group.MapPost("/{tutorId:guid}/sessions/{sessionId:guid}/rate", HandleAsync);
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