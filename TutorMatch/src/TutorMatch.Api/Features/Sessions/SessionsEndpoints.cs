using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Features.Sessions.BookSession;
using TutorMatch.Api.Features.Sessions.CancelSession;
using TutorMatch.Api.Features.Sessions.CompleteSession;
using TutorMatch.Api.Features.Sessions.DeleteSession;
using TutorMatch.Api.Features.Sessions.GetMySessions;
using TutorMatch.Api.Features.Sessions.GetSessionById;
using TutorMatch.Api.Features.Sessions.GetSessions;
using TutorMatch.Api.Features.Sessions.UpdateSession;

namespace TutorMatch.Api.Features.Sessions;

// Minimal API endpoint wiring is not unit-testable and is excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class SessionsEndpoints
{
    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder app)
    {
        // Version 2 (Current / Next Version) - with Subject selection & Details
        var v2Group = app.MapGroup("/api/v2/sessions").WithTags("Sessions v2");
        MapRoutes(v2Group);

        // Version 1 (Previous Version)
        var v1Group = app.MapGroup("/api/v1/sessions").WithTags("Sessions v1");
        MapRoutes(v1Group);

        // Legacy / Unversioned fallback
        var legacyGroup = app.MapGroup("/api/sessions").WithTags("Sessions");
        MapRoutes(legacyGroup);

        return app;
    }

    private static void MapRoutes(RouteGroupBuilder group)
    {
        group.MapBookSession();
        group.MapGetMySessions();
        group.MapGetSessionById();
        group.MapGetSessions();
        group.MapUpdateSession();
        group.MapDeleteSession();
        group.MapCancelSession();
        group.MapCompleteSession();
    }
}
