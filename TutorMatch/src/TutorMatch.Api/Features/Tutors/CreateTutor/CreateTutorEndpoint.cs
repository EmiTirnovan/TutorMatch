using Microsoft.AspNetCore.Http.HttpResults;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Tutors;

namespace TutorMatch.Api.Features.Tutors.CreateTutor;

public static class CreateTutorEndpoint
{
    public static IEndpointRouteBuilder MapCreateTutor(this IEndpointRouteBuilder group)
    {
        group.MapPost("/", Handle);
        return group;
    }

    private static Results<Created<CreateTutorResponse>, BadRequest<string>> Handle(
        CreateTutorRequest request,
        ITutorRepository repository)
    {
        try
        {
            // Instantiem agregatul. Constructorul va valida datele si va arunca exceptii daca e ceva in neregula.
            var tutor = new Tutor(
                request.Name,
                request.Email,
                request.Bio,
                request.HourlyRate);

            if (request.SubjectIds is not null)
            {
                foreach (var subjectId in request.SubjectIds)
                {
                    tutor.AddSubject(subjectId);
                }
            }

            if (request.Availability is not null)
            {
                foreach (var slot in request.Availability)
                {
                    tutor.AddAvailability(new DateRange(slot.Start, slot.End));
                }
            }

            // Cum ID-ul este generat intern ca un nou Guid la initializare, nu vom avea conflicte.
            repository.Add(tutor);

            var response = new CreateTutorResponse(
                tutor.Id, tutor.Name, tutor.Email, tutor.Bio, tutor.HourlyRate);

            return TypedResults.Created($"/api/tutors/{tutor.Id}", response);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            // Orice eroare de domeniu se transforma curat intr-un 400 Bad Request
            return TypedResults.BadRequest(ex.Message);
        }
    }
}