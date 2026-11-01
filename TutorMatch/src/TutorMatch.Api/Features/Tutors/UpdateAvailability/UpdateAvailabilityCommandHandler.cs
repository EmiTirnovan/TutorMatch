namespace TutorMatch.Api.Features.Tutors.UpdateAvailability;

using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Domain.Tutors.Events;

public class UpdateAvailabilityCommandHandler : ICommandHandler<UpdateAvailabilityCommand>
{
    private readonly ITutorRepository _repository;

    public UpdateAvailabilityCommandHandler(ITutorRepository repository)
    {
        _repository = repository;
    }

    public Task<Result> HandleAsync(UpdateAvailabilityCommand command, CancellationToken cancellationToken = default)
    {
        var tutor = _repository.GetById(command.TutorId);
        if (tutor is null)
        {
            return Task.FromResult(Result.Failure(
                Error.NotFound("Tutor.NotFound", $"Tutor with ID '{command.TutorId}' was not found.")));
        }

        try
        {
            var ranges = (command.Availability ?? new())
                .Select(dto => new DateRange(dto.Start, dto.End))
                .ToList();

            tutor.UpdateAvailability(ranges);
            _repository.Update(tutor);

            // Publish TutorSavedNotification for in-process subscribers like AI embedding generation
            var notification = tutor.CreateTutorSavedNotification();
            tutor.AddDomainEvent(notification);
            TutorSavedNotification.Publish(notification);

            return Task.FromResult(Result.Success());
        }
        catch (ArgumentException ex)
        {
            return Task.FromResult(Result.Failure(
                Error.Validation("Tutor.Validation", ex.Message)));
        }
        catch (InvalidOperationException ex)
        {
            return Task.FromResult(Result.Failure(
                Error.Validation("Tutor.Validation", ex.Message)));
        }
        catch (KeyNotFoundException ex)
        {
            return Task.FromResult(Result.Failure(
                Error.NotFound("Tutor.NotFound", ex.Message)));
        }
    }
}
