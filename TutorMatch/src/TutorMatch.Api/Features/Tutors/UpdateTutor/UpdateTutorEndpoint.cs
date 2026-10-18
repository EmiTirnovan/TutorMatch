using Microsoft.AspNetCore.Http.HttpResults;
using TutorMatch.Api.Domain.Tutors;

namespace TutorMatch.Api.Features.Tutors.UpdateTutor;

public static class UpdateTutorEndpoint
{
    public static IEndpointRouteBuilder MapUpdateTutor(this IEndpointRouteBuilder group)
    {
        group.MapPut("/{id:guid}", Handle);
        return group;
    }

    private static Results<NoContent, NotFound, BadRequest<string>> Handle(
        Guid id,
        UpdateTutorRequest request,
        ITutorRepository repository)
    {
        var tutor = repository.GetById(id);
        if (tutor is null)
        {
            return TypedResults.NotFound();
        }

        try
        {
            tutor.UpdateProfile(request.Name, request.Bio, request.HourlyRate);
            
            repository.Update(tutor);

            return TypedResults.NoContent();
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }
}