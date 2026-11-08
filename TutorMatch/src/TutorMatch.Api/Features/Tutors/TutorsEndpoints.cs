using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Features.SemanticSearch;
using TutorMatch.Api.Features.Tutors.CreateTutor;
using TutorMatch.Api.Features.Tutors.DeleteTutor;
using TutorMatch.Api.Features.Tutors.GetTutorById;
using TutorMatch.Api.Features.Tutors.GetTutors;
using TutorMatch.Api.Features.Tutors.UpdateAvailability;
using TutorMatch.Api.Features.Tutors.UpdateTutor;

namespace TutorMatch.Api.Features.Tutors;

// Minimal API endpoint wiring is not unit-testable and is excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class TutorsEndpoints
{
    public static IEndpointRouteBuilder MapTutorEndpoints(this IEndpointRouteBuilder app)
    {
        var v2Group = app.MapGroup("/api/v2/tutors").WithTags("Tutors v2");
        MapRoutes(v2Group);

        var v1Group = app.MapGroup("/api/v1/tutors").WithTags("Tutors v1");
        MapRoutes(v1Group);

        var legacyGroup = app.MapGroup("/api/tutors").WithTags("Tutors");
        MapRoutes(legacyGroup);

        return app;
    }

    private static void MapRoutes(RouteGroupBuilder group)
    {
        group.MapCreateTutor();
        group.MapGetTutorById();
        group.MapGetTutors();
        group.MapUpdateTutor();
        group.MapUpdateAvailability();
        group.MapDeleteTutor();
        group.MapSemanticSearch();
    }
}
