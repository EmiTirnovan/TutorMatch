using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Students.UpdateProfile;

// Minimal API endpoint wiring is not unit-testable and is excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class UpdateStudentProfileEndpoint
{
    public static IEndpointRouteBuilder MapUpdateStudentProfile(this IEndpointRouteBuilder group)
    {
        group.MapPut("/{id:guid}", HandleAsync)
            .WithSummary("Update a student profile")
            .WithDescription("Updates the personal profile details for an existing student.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/profile", HandleAsync)
            .WithSummary("Update a student profile (alias)")
            .WithDescription("Updates the personal profile details for an existing student.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateStudentProfileRequest request,
        ICommandHandler<UpdateStudentProfileCommand> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateStudentProfileCommand(id, request.Name);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToHttpResult();
    }
}
