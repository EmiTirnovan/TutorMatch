using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Features.Sessions.GetMySessions;

// Minimal API endpoint wiring is not unit-testable and is excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class GetMySessionsEndpoint
{
    public static IEndpointRouteBuilder MapGetMySessions(this IEndpointRouteBuilder group)
    {
        group.MapGet("/my", HandleAsync);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] Guid userId,
        [FromQuery] string role,
        [FromQuery] string? status,
        IQueryHandler<GetMySessionsQuery, Result<GetMySessionsResponse>> handler,
        CancellationToken ct)
    {
        SessionStatus? sessionStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals("Upcoming", StringComparison.OrdinalIgnoreCase))
            {
                sessionStatus = SessionStatus.Booked;
            }
            else if (Enum.TryParse<SessionStatus>(status, true, out var parsedStatus))
            {
                sessionStatus = parsedStatus;
            }
            else
            {
                var error = Error.Validation(
                    "Session.Validation",
                    $"Invalid status '{status}'. Allowed values are Upcoming, Booked, Cancelled, LateCancelled, Completed.");
                return Result<GetMySessionsResponse>.Failure(error).ToHttpResult();
            }
        }

        var query = new GetMySessionsQuery(userId, role, sessionStatus);
        var result = await handler.HandleAsync(query, ct);
        return result.ToHttpResult();
    }
}

