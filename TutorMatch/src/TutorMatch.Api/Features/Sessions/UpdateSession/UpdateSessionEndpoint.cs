using Microsoft.AspNetCore.Http.HttpResults;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Features.Sessions.UpdateSession;

public static class UpdateSessionEndpoint
{
    public static IEndpointRouteBuilder MapUpdateSession(this IEndpointRouteBuilder group)
    {
        group.MapPut("/{id:guid}", Handle);
        return group;
    }

    private static Results<NoContent, NotFound, BadRequest<string>> Handle(
        Guid id,
        UpdateSessionRequest request,
        ISessionRepository repository)
    {
        var session = repository.GetById(id);

        if (session is null)
        {
            return TypedResults.NotFound();
        }

        try
        {
            session.Reschedule(new DateRange(request.StartTime, request.EndTime));
            repository.Update(session);
            return TypedResults.NoContent();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }
}
