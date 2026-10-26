using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Tutors.UpdateTutor;

// Minimal API endpoint wiring is not unit-testable and is excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class UpdateTutorEndpoint
{
    public static IEndpointRouteBuilder MapUpdateTutor(this IEndpointRouteBuilder group)
    {
        group.MapPut("/{id:guid}", HandleAsync);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateTutorRequest request,
        ICommandHandler<UpdateTutorCommand> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTutorCommand(id, request.Name, request.Bio, request.HourlyRate);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToHttpResult();
    }
}
