using Microsoft.AspNetCore.Http;
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
        group.MapPost("/", HandleAsync);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        BookSessionRequest request,
        ICommandHandler<BookSessionCommand, Result<BookSessionResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new BookSessionCommand(
            request.TutorId,
            request.StudentId,
            request.StartTime,
            request.EndTime);

        var result = await handler.HandleAsync(command, cancellationToken);

        return result.ToHttpResult(response => TypedResults.Created($"/api/sessions/{response.Id}", response));
    }
}
