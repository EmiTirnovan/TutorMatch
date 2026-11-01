using Microsoft.AspNetCore.Routing;

namespace TutorMatch.Api.Features.SemanticSearch;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class SemanticSearchEndpoints
{
    public static IEndpointRouteBuilder MapSemanticSearchEndpoints(this IEndpointRouteBuilder app)
    {
        return SemanticSearchEndpoint.MapSemanticSearchEndpoints(app);
    }
}

