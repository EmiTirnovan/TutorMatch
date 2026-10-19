using Microsoft.AspNetCore.Http.HttpResults;
using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Features.Sessions.GetSessions;

public static class GetSessionsEndpoint
{
    public static IEndpointRouteBuilder MapGetSessions(this IEndpointRouteBuilder group)
    {
        group.MapGet("/", Handle);
        return group;
    }

    private static Ok<List<GetSessionsResponse>> Handle(ISessionRepository repository)
    {
        var sessions = repository.GetAll()
            .Select(session => new GetSessionsResponse(
                session.Id,
                session.TutorId,
                session.StudentId,
                session.StartTime,
                session.EndTime,
                session.Status,
                session.Version))
            .ToList();

        return TypedResults.Ok(sessions);
    }
}
