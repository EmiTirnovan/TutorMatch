namespace TutorMatch.Api.Features.Tutors.UpdateAvailability;

using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Domain.Tutors.Events;
using TutorMatch.Api.Domain.Users;

public class UpdateAvailabilityCommandHandler : ICommandHandler<UpdateAvailabilityCommand>
{
    private readonly ITutorRepository _repository;
    private readonly IUserRepository? _userRepository;

    public UpdateAvailabilityCommandHandler(
        ITutorRepository repository,
        IUserRepository? userRepository = null)
    {
        _repository = repository;
        _userRepository = userRepository;
    }

    public Task<Result> HandleAsync(UpdateAvailabilityCommand command, CancellationToken cancellationToken = default)
    {
        var tutor = _repository.GetById(command.TutorId);
        if (tutor is null && _userRepository is not null)
        {
            var user = _userRepository.GetById(command.TutorId);
            if (user is not null && user.Role.Equals("Tutor", StringComparison.OrdinalIgnoreCase))
            {
                tutor = new Tutor(user.FullName, user.Email, "Tutor on TutorMatch", 50m, user.Id);
                _repository.Add(tutor);
            }
        }

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

            // Record domain event on aggregate prior to persistence
            var notification = tutor.CreateTutorSavedNotification();
            tutor.AddDomainEvent(notification);

            // Persist to database; if persistence fails, no event is dispatched
            _repository.Update(tutor);

            // Dispatch domain events strictly post-commit
            TutorSavedNotification.DispatchPostCommitEvents(tutor);

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
