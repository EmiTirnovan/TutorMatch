using Microsoft.AspNetCore.Http.HttpResults;
using TutorMatch.Api.Domain.Tutors;

namespace TutorMatch.Api.Features.Tutors.DeleteTutor;

public static class DeleteTutorEndpoint
{
    public static IEndpointRouteBuilder MapDeleteTutor(this IEndpointRouteBuilder group)
    {
        group.MapDelete("/{id:guid}", Handle);
        return group;
    }

    private static Results<NoContent, NotFound> Handle(
        Guid id,
        ITutorRepository repository)
    {
        if (!repository.Delete(id))
        {
            return TypedResults.NotFound();
        }

        return TypedResults.NoContent();
    }
}