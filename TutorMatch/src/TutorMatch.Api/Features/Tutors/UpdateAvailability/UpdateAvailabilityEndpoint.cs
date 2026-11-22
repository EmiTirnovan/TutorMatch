namespace TutorMatch.Api.Features.Tutors.UpdateAvailability;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Dtos;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Tutors;

// Minimal API endpoint wiring is not unit-testable and is excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class UpdateAvailabilityEndpoint
{
    public static IEndpointRouteBuilder MapUpdateAvailability(this IEndpointRouteBuilder group)
    {
        group.MapGet("/{id:guid}/availability", HandleGetAsync)
            .WithSummary("Get a tutor's availability slots")
            .Produces<List<DateRangeDto>>(StatusCodes.Status200OK)
            .ProducesErrors(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/availability", HandleAsync)
            .WithSummary("Replace a tutor's availability")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound);
        return group;
    }

    private static Task<IResult> HandleGetAsync(
        Guid id,
        ITutorRepository repository)
    {
        var tutor = repository.GetById(id);
        if (tutor is null)
        {
            return Task.FromResult(Results.NotFound(new { error = $"Tutor with ID '{id}' was not found." }));
        }

        var dtos = tutor.Availability
            .Select(a => new DateRangeDto(a.Start, a.End))
            .ToList();

        return Task.FromResult(Results.Ok(dtos));
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
