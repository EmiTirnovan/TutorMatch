using Microsoft.AspNetCore.Http.HttpResults;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Features.Tutors.CreateTutor;

namespace TutorMatch.Api.Features.Tutors.GetTutorById;

public static class GetTutorByIdEndpoint
{
    public static IEndpointRouteBuilder MapGetTutorById(this IEndpointRouteBuilder group)
    {
        group.MapGet("/{id:guid}", Handle);
        return group;
    }

    private static Results<Ok<GetTutorByIdResponse>, NotFound> Handle(
        Guid id,
        ITutorRepository repository)
    {
        var tutor = repository.GetById(id);

        if (tutor is null)
        {
            return TypedResults.NotFound();
        }

        var response = new GetTutorByIdResponse(
            tutor.Id,
            tutor.Name,
            tutor.Email,
            tutor.Bio,
            tutor.HourlyRate,
            tutor.TutorSubjects.Select(ts => ts.SubjectId).ToList(),
            tutor.Availability.Select(a => new DateRangeDto(a.Start, a.End)).ToList()
        );

        return TypedResults.Ok(response);
    }
}