using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Sessions.BookSession;

// Minimal API endpoint wiring is not unit-testable and is excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class BookSessionEndpoint
{
    public static IEndpointRouteBuilder MapBookSession(this IEndpointRouteBuilder group)
    {
        group.MapPost("/", HandleAsync)
            .WithSummary("Book a tutoring session")
            .WithDescription("Books a session for the student identified by the X-Student-Id header. Online sessions get a generated Google Meet link; physical sessions require LocationName, Latitude and Longitude. Returns 500 (Session.MeetingLinkFailed) when the meeting link cannot be created.")
            .Produces<BookSessionResponse>(StatusCodes.Status201Created)
            .ProducesErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError)
            .WithOpenApi(operation =>
            {
                operation.Parameters.First(p => p.Name == "X-Student-Id").Description = "Id of the student making the request (no auth yet).";
                return operation;
            });
        return group;
    }

    private static async Task<IResult> HandleAsync(
        [FromHeader(Name = "X-Student-Id")] Guid studentId,
        BookSessionRequest request,
        ICommandHandler<BookSessionCommand, Result<BookSessionResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new BookSessionCommand(
            request.TutorId,
            studentId,
            request.StartTime,
            request.EndTime,
            request.Subject,
            request.Notes,
            request.Type,
            request.LocationName,
            request.Latitude,
            request.Longitude);

        var result = await handler.HandleAsync(command, cancellationToken);

        return result.ToHttpResult(response => TypedResults.Created($"/api/sessions/{response.Id}", response));
    }
}
