using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Sessions.DeleteSession;

// Minimal API endpoint wiring is not unit-testable and is excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class DeleteSessionEndpoint
{
    public static IEndpointRouteBuilder MapDeleteSession(this IEndpointRouteBuilder group)
    {
        group.MapDelete("/{id:guid}", HandleAsync)
            .RequireAuthorization()
            .WithSummary("Delete a session")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesErrors(StatusCodes.Status404NotFound);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ICommandHandler<DeleteSessionCommand> handler,
        CancellationToken cancellationToken)
    {
        var command = new DeleteSessionCommand(id);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToHttpResult();
    }
}
