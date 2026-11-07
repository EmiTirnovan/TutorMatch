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
        var group = app.MapGroup("/api/sessions");

        group.MapBookSession();
        group.MapGetMySessions();
        group.MapGetSessionById();
        group.MapGetSessions();
        group.MapUpdateSession();
        group.MapDeleteSession();
        group.MapCancelSession();
        group.MapCompleteSession();

        return app;
    }
}
