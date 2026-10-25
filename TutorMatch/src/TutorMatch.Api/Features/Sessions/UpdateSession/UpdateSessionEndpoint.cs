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
        group.MapPut("/{id:guid}", HandleAsync);
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
