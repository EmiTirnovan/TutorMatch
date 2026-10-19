using Microsoft.AspNetCore.Http.HttpResults;
using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Features.Sessions.DeleteSession;

public static class DeleteSessionEndpoint
{
    public static IEndpointRouteBuilder MapDeleteSession(this IEndpointRouteBuilder group)
    {
        group.MapDelete("/{id:guid}", Handle);
        return group;
    }

    private static Results<NoContent, NotFound> Handle(
        Guid id,
        ISessionRepository repository)
    {
        if (!repository.Delete(id))
        {
            return TypedResults.NotFound();
        }

        return TypedResults.NoContent();
    }
}
