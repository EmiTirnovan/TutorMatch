using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace TutorMatch.Api.Features.SemanticSearch;

// Rate limiter registration is bootstrapping glue and is excluded from unit-test coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class SemanticSearchRateLimiting
{
    public const string PolicyName = "semantic-search";
    public const int PermitLimit = 15;
    public const string RejectionCode = "SemanticSearch.RateLimited";
    public const string RejectionDescription =
        "Prea multe cautari semantice intr-un minut. Incearca din nou peste cateva secunde.";

    public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Limits /semantic-search to 15 requests per client IP per sliding minute (no queueing).
    /// Rejected requests get HTTP 429 with a Retry-After header and a { code, description } body.
    /// </summary>
    public static IServiceCollection AddSemanticSearchRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { Code = RejectionCode, Description = RejectionDescription },
                    cancellationToken);
            };

            options.AddPolicy(PolicyName, httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = PermitLimit,
                        Window = Window,
                        SegmentsPerWindow = 6,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
        });

        return services;
    }
}
