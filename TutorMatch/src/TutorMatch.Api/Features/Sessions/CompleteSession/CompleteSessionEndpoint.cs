using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Sessions.CompleteSession;

// Minimal API endpoint wiring is not unit-testable and is excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class CompleteSessionEndpoint
{
    public static IEndpointRouteBuilder MapCompleteSession(this IEndpointRouteBuilder group)
    {
        group.MapPost("/{id:guid}/complete", HandleAsync);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        [FromBody] CompleteSessionRequest? request,
        [FromQuery] Guid? tutorId,
        ICommandHandler<CompleteSessionCommand> handler,
        CancellationToken ct)
    {
        var actualTutorId = request?.TutorId ?? tutorId ?? Guid.Empty;
        var command = new CompleteSessionCommand(id, actualTutorId);

        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult();
    }
}

