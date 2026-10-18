using Microsoft.AspNetCore.Http.HttpResults;
using TutorMatch.Api.Domain.Tutors;

namespace TutorMatch.Api.Features.Tutors.GetTutors;

public static class GetTutorsEndpoint
{
    public static IEndpointRouteBuilder MapGetTutors(this IEndpointRouteBuilder group)
    {
        group.MapGet("/", Handle);
        return group;
    }

    private static Ok<List<TutorSummaryResponse>> Handle(ITutorRepository repository)
    {
        var tutors = repository.GetAll();
        
        var response = tutors.Select(t => new TutorSummaryResponse(
            t.Id, t.Name, t.Email, t.HourlyRate)).ToList();

        return TypedResults.Ok(response);
    }
}