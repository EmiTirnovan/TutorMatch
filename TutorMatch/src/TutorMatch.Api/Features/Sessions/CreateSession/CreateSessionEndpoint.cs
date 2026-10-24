using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Sessions.CreateSession;

public static class CreateSessionEndpoint
{
    public static IEndpointRouteBuilder MapCreateSession(this IEndpointRouteBuilder group)
    {
        group.MapPost("/", HandleAsync);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        CreateSessionRequest request,
        ICommandHandler<CreateSessionCommand, Result<CreateSessionResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateSessionCommand(
            request.TutorId,
            request.StudentId,
            request.StartTime,
            request.EndTime);

        var result = await handler.HandleAsync(command, cancellationToken);

        return result.ToHttpResult(response => TypedResults.Created($"/api/sessions/{response.Id}", response));
    }
}
