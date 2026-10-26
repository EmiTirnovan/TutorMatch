using TutorMatch.Api.Features.Tutors.CreateTutor;
using TutorMatch.Api.Features.Tutors.DeleteTutor;
using TutorMatch.Api.Features.Tutors.GetTutorById;
using TutorMatch.Api.Features.Tutors.GetTutors;
using TutorMatch.Api.Features.Tutors.UpdateTutor;

namespace TutorMatch.Api.Features.Tutors;

// Minimal API endpoint wiring is not unit-testable and is excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class TutorsEndpoints
{
    public static IEndpointRouteBuilder MapTutorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tutors");

        group.MapCreateTutor();
        group.MapGetTutorById();
        group.MapGetTutors();
        group.MapUpdateTutor();
        group.MapDeleteTutor();

        return app;
    }
}