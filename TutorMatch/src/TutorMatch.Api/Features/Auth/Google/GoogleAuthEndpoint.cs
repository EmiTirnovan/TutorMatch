using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Auth.Google;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class GoogleAuthEndpoint
{
    public static IEndpointRouteBuilder MapGoogleAuth(this IEndpointRouteBuilder group)
    {
        group.MapPost("/google", HandleAsync)
            .WithSummary("Authenticate or register via Google ID Token")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .ProducesErrors(StatusCodes.Status400BadRequest);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        GoogleAuthRequest request,
        ICommandHandler<GoogleAuthCommand, Result<AuthResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new GoogleAuthCommand(request.Token, request.Role);
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.ToHttpResult();
    }
}
