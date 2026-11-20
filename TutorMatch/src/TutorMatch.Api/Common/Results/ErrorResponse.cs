using Microsoft.AspNetCore.Builder;

namespace TutorMatch.Api.Common.Results;

/// <summary>Body returned by <see cref="ResultExtensions.ToHttpResult(Result)"/> for every failed result.</summary>
public sealed record ErrorResponse(string Code, string Description);

// Minimal API metadata wiring is not unit-testable and is excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class ErrorResponseEndpointExtensions
{
    /// <summary>Documents the error status codes an endpoint can return through <c>ToHttpResult</c>.</summary>
    public static RouteHandlerBuilder ProducesErrors(this RouteHandlerBuilder builder, params int[] statusCodes)
    {
        foreach (var statusCode in statusCodes)
        {
            builder.Produces<ErrorResponse>(statusCode);
        }

        return builder;
    }
}
