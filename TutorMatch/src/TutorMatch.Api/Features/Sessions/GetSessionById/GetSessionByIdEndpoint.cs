using Microsoft.AspNetCore.Http.HttpResults;
using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Features.Sessions.GetSessionById;

public static class GetSessionByIdEndpoint
{
    public static IEndpointRouteBuilder MapGetSessionById(this IEndpointRouteBuilder group)
    {
        group.MapGet("/{id:guid}", Handle);
        return group;
    }

    private static Results<Ok<GetSessionByIdResponse>, NotFound> Handle(
        Guid id,
        ISessionRepository repository)
    {
        var session = repository.GetById(id);

        if (session is null)
        {
            return TypedResults.NotFound();
        }

        var response = new GetSessionByIdResponse(
            session.Id,
            session.TutorId,
            session.StudentId,
            session.StartTime,
            session.EndTime,
            session.Status,
            session.Version);

        return TypedResults.Ok(response);
    }
}
