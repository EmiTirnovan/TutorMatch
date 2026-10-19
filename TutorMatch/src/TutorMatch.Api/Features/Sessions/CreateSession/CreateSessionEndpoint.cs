using Microsoft.AspNetCore.Http.HttpResults;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Features.Sessions.CreateSession;

public static class CreateSessionEndpoint
{
    public static IEndpointRouteBuilder MapCreateSession(this IEndpointRouteBuilder group)
    {
        group.MapPost("/", Handle);
        return group;
    }

    private static Results<Created<CreateSessionResponse>, BadRequest<string>> Handle(
        CreateSessionRequest request,
        ISessionRepository repository)
    {
        if (request.TutorId == Guid.Empty || request.StudentId == Guid.Empty)
        {
            return TypedResults.BadRequest("TutorId and StudentId must be valid GUIDs.");
        }

        try
        {
            var interval = new DateRange(request.StartTime, request.EndTime);
            var session = Session.Create(request.TutorId, request.StudentId, interval);

            repository.Add(session);

            var response = new CreateSessionResponse(
                session.Id,
                session.TutorId,
                session.StudentId,
                session.StartTime,
                session.EndTime,
                session.Status,
                session.Version);

            return TypedResults.Created($"/api/sessions/{session.Id}", response);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }
}
