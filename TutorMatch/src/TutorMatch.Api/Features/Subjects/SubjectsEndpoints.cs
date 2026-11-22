using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Features.Subjects.SuggestSubjects;

namespace TutorMatch.Api.Features.Subjects;

// Minimal API endpoint wiring is not unit-testable and is excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class SubjectsEndpoints
{
    public static IEndpointRouteBuilder MapSubjectsEndpoints(this IEndpointRouteBuilder app)
    {
        var v2Group = app.MapGroup("/api/v2/subjects").WithTags("Subjects").WithGroupName("v2");
        MapRoutes(v2Group);

        var v1Group = app.MapGroup("/api/v1/subjects").WithTags("Subjects").WithGroupName("v1");
        MapRoutes(v1Group);

        var legacyGroup = app.MapGroup("/api/subjects").WithTags("Subjects").WithGroupName("v2");
        MapRoutes(legacyGroup);

        return app;
    }

    private static void MapRoutes(RouteGroupBuilder group)
    {
        group.MapSuggestSubjects();
    }
}
