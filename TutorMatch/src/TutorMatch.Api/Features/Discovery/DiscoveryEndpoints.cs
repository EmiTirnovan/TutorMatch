using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using TutorMatch.Api.Features.Discovery.SearchTutors;
using TutorMatch.Api.Features.Discovery.RateSession;
namespace TutorMatch.Api.Features.Discovery;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class DiscoveryEndpoints
{
    public static IEndpointRouteBuilder MapDiscoveryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tutors");
        
        group.MapSearchTutors();
        group.MapRateSession();
        
        return app;
    }
}