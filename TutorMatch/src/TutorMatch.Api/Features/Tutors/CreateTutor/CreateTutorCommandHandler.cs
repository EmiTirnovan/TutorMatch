using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Domain.Tutors.Events;

namespace TutorMatch.Api.Features.Tutors.CreateTutor;

public class CreateTutorCommandHandler : ICommandHandler<CreateTutorCommand, Result<CreateTutorResponse>>
{
    private readonly ITutorRepository _repository;

    public CreateTutorCommandHandler(ITutorRepository repository)
    {
        _repository = repository;
    }

    public Task<Result<CreateTutorResponse>> HandleAsync(
        CreateTutorCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            var tutor = new Tutor(
                command.Name,
                command.Email,
                command.Bio,
                command.HourlyRate);

            if (command.SubjectIds is not null)
            {
                foreach (var subjectId in command.SubjectIds)
                {
                    tutor.AddSubject(subjectId);
                }
            }

            if (command.Availability is not null)
            {
                foreach (var slot in command.Availability)
                {
                    tutor.AddAvailability(new DateRange(slot.Start, slot.End));
                }
            }

            // Record domain event on aggregate prior to persistence
            var notification = tutor.CreateTutorSavedNotification();
            tutor.AddDomainEvent(notification);

            // Persist to database; if persistence fails, no event is dispatched
            _repository.Add(tutor);

            // Dispatch domain events strictly post-commit
            TutorSavedNotification.DispatchPostCommitEvents(tutor);

            var response = new CreateTutorResponse(
                tutor.Id,
                tutor.Name,
                tutor.Email,
                tutor.Bio,
                tutor.HourlyRate);

            return Task.FromResult(Result<CreateTutorResponse>.Success(response));
        }
        catch (ArgumentException ex)
        {
            return Task.FromResult(Result<CreateTutorResponse>.Failure(
                Error.Validation("Tutor.Validation", ex.Message)));
        }
        catch (InvalidOperationException ex)
        {
            return Task.FromResult(Result<CreateTutorResponse>.Failure(
                Error.Conflict("Tutor.Conflict", ex.Message)));
        }
    }
}
