using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using TutorMatch.Api.Features.Students.GetStudentById;
using TutorMatch.Api.Features.Students.UpdateProfile;

namespace TutorMatch.Api.Features.Students;

// Minimal API endpoint wiring is not unit-testable and is excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class StudentsEndpoints
{
    public static IEndpointRouteBuilder MapStudentsEndpoints(this IEndpointRouteBuilder app)
    {
        var v2Group = app.MapGroup("/api/v2/students").WithTags("Students").WithGroupName("v2");
        MapRoutes(v2Group);

        var v1Group = app.MapGroup("/api/v1/students").WithTags("Students").WithGroupName("v1");
        MapRoutes(v1Group);

        var legacyGroup = app.MapGroup("/api/students").WithTags("Students").WithGroupName("v2");
        MapRoutes(legacyGroup);

        return app;
    }

    private static void MapRoutes(RouteGroupBuilder group)
    {
        group.MapGetStudentById();
        group.MapUpdateStudentProfile();
    }
}
