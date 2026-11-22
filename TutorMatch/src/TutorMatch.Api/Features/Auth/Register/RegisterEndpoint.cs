using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Auth.Register;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class RegisterEndpoint
{
    public static IEndpointRouteBuilder MapRegister(this IEndpointRouteBuilder group)
    {
        group.MapPost("/register", HandleAsync)
            .WithSummary("Register a new user with email and password")
            .Produces<AuthResponse>(StatusCodes.Status201Created)
            .ProducesErrors(StatusCodes.Status400BadRequest, StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        RegisterRequest request,
        ICommandHandler<RegisterCommand, Result<AuthResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(request.Email, request.Password, request.FullName, request.Role);
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.ToHttpResult(response => TypedResults.Created($"/api/users/{response.UserId}", response));
    }
}
