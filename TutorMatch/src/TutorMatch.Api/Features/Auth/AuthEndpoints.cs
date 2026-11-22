using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using TutorMatch.Api.Features.Auth.Google;
using TutorMatch.Api.Features.Auth.Login;
using TutorMatch.Api.Features.Auth.Register;
using TutorMatch.Api.Infrastructure.Security;

namespace TutorMatch.Api.Features.Auth;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/api/auth").WithTags("Authentication");

        authGroup.MapRegister();
        authGroup.MapLogin();
        authGroup.MapGoogleAuth();

        authGroup.MapGet("/config", (IOptions<GoogleAuthOptions> googleOptions) =>
        {
            return TypedResults.Ok(new
            {
                GoogleClientId = googleOptions.Value.ClientId ?? string.Empty
            });
        })
        .AllowAnonymous()
        .WithSummary("Get public client configuration")
        .Produces(StatusCodes.Status200OK);

        authGroup.MapGet("/me", (ClaimsPrincipal principal) =>
        {
            var userId = principal.FindFirst("nameid")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = principal.FindFirst("email")?.Value ?? principal.FindFirst(ClaimTypes.Email)?.Value;
            var name = principal.FindFirst("name")?.Value ?? principal.FindFirst(ClaimTypes.Name)?.Value;
            var role = principal.FindFirst("role")?.Value ?? principal.FindFirst(ClaimTypes.Role)?.Value;

            return TypedResults.Ok(new
            {
                UserId = userId,
                Email = email,
                FullName = name,
                Role = role
            });
        })
        .RequireAuthorization()
        .WithSummary("Get authenticated user profile from token claims")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }
}
