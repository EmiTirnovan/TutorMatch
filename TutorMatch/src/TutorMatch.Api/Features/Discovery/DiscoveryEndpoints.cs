using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using TutorMatch.Api.Features.Discovery.SearchTutors;
using TutorMatch.Api.Features.Discovery.RateSession;
using TutorMatch.Api.Features.SemanticSearch;

namespace TutorMatch.Api.Features.Discovery;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class DiscoveryEndpoints
{
    public static IEndpointRouteBuilder MapDiscoveryEndpoints(this IEndpointRouteBuilder app)
    {
        MapRoutes(app.MapGroup("/api/v2/tutors").WithTags("Discovery / Search").WithGroupName("v2"));
        MapRoutes(app.MapGroup("/api/v1/tutors").WithTags("Discovery / Search").WithGroupName("v1"));
        MapRoutes(app.MapGroup("/api/tutors").WithTags("Discovery / Search (unversioned)").WithGroupName("v1"));

        return app;
    }

    private static void MapRoutes(RouteGroupBuilder group)
    {
        group.MapSearchTutors();
        group.MapRateSession();
        group.MapSemanticSearch();
    }
}
