using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Sessions.UpdateSession;

// Minimal API endpoint wiring is not unit-testable and is excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class UpdateSessionEndpoint
{
    public static IEndpointRouteBuilder MapUpdateSession(this IEndpointRouteBuilder group)
    {
        group.MapPut("/{id:guid}", HandleAsync)
            .WithSummary("Reschedule a session")
            .WithDescription("Changes the start and end time of a booked session.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateSessionRequest request,
        ICommandHandler<UpdateSessionCommand> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateSessionCommand(id, request.StartTime, request.EndTime);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToHttpResult();
    }
}
