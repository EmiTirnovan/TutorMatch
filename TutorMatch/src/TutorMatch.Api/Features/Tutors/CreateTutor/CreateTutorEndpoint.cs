using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Tutors.CreateTutor;

// Minimal API endpoint wiring is not unit-testable and is excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class CreateTutorEndpoint
{
    public static IEndpointRouteBuilder MapCreateTutor(this IEndpointRouteBuilder group)
    {
        group.MapPost("/", HandleAsync);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        CreateTutorRequest request,
        ICommandHandler<CreateTutorCommand, Result<CreateTutorResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateTutorCommand(
            request.Name,
            request.Email,
            request.Bio,
            request.HourlyRate,
            request.SubjectIds,
            request.Availability);

        var result = await handler.HandleAsync(command, cancellationToken);

        return result.ToHttpResult(response => TypedResults.Created($"/api/tutors/{response.Id}", response));
    }
}
