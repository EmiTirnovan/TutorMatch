namespace TutorMatch.Api.Features.Tutors.UpdateAvailability;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

// Minimal API endpoint wiring is not unit-testable and is excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class UpdateAvailabilityEndpoint
{
    public static IEndpointRouteBuilder MapUpdateAvailability(this IEndpointRouteBuilder group)
    {
        group.MapPut("/{id:guid}/availability", HandleAsync);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateAvailabilityRequest request,
        ICommandHandler<UpdateAvailabilityCommand> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateAvailabilityCommand(id, request.Availability);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToHttpResult();
    }
}
