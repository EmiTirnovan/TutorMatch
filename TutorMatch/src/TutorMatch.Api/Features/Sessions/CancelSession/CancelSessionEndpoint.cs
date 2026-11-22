using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Sessions.CancelSession;

// Minimal API endpoint wiring is not unit-testable and is excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class CancelSessionEndpoint
{
    public static IEndpointRouteBuilder MapCancelSession(this IEndpointRouteBuilder group)
    {
        group.MapPost("/{id:guid}/cancel", HandleAsync)
            .RequireAuthorization()
            .WithSummary("Cancel a session")
            .WithDescription("The requester id can be sent in the body or as the requesterId query parameter.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        [FromBody] CancelSessionRequest? request,
        [FromQuery] Guid? requesterId,
        ICommandHandler<CancelSessionCommand> handler,
        CancellationToken ct)
    {
        var actualRequesterId = request?.RequesterId ?? requesterId ?? Guid.Empty;
        var command = new CancelSessionCommand(id, actualRequesterId);

        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult();
    }
}

