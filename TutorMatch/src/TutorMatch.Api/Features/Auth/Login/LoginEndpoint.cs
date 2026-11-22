using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Auth.Login;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class LoginEndpoint
{
    public static IEndpointRouteBuilder MapLogin(this IEndpointRouteBuilder group)
    {
        group.MapPost("/login", HandleAsync)
            .WithSummary("Log in with email and password")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .ProducesErrors(StatusCodes.Status400BadRequest);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        LoginRequest request,
        ICommandHandler<LoginCommand, Result<AuthResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.ToHttpResult();
    }
}
