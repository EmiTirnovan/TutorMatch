using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Domain.Tutors.Events;
using TutorMatch.Api.Domain.Users;

namespace TutorMatch.Api.Features.Tutors.UpdateTutor;

public class UpdateTutorCommandHandler : ICommandHandler<UpdateTutorCommand>
{
    private readonly ITutorRepository _repository;
    private readonly IUserRepository? _userRepository;

    public UpdateTutorCommandHandler(
        ITutorRepository repository,
        IUserRepository? userRepository = null)
    {
        _repository = repository;
        _userRepository = userRepository;
    }

    public Task<Result> HandleAsync(UpdateTutorCommand command, CancellationToken cancellationToken = default)
    {
        var tutor = _repository.GetById(command.Id);
        if (tutor is null && _userRepository is not null)
        {
            var user = _userRepository.GetById(command.Id);
            if (user is not null && user.Role.Equals("Tutor", StringComparison.OrdinalIgnoreCase))
            {
                tutor = new Tutor(user.FullName, user.Email, "Tutor on TutorMatch", 50m, user.Id);
                _repository.Add(tutor);
            }
        }

        if (tutor is null)
        {
            return Task.FromResult(Result.Failure(
                Error.NotFound("Tutor.NotFound", $"Tutor with ID '{command.Id}' was not found.")));
        }

        try
        {
            tutor.UpdateProfile(command.Name, command.Bio, command.HourlyRate);

            if (command.Subjects is not null)
            {
                var subjects = command.Subjects
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => _repository.GetOrCreateSubject(s))
                    .ToList();
                tutor.UpdateSubjects(subjects);
            }

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
        catch (KeyNotFoundException ex)
        {
            return Task.FromResult(Result.Failure(
                Error.NotFound("Tutor.NotFound", ex.Message)));
        }
        catch (InvalidOperationException ex)
        {
            return Task.FromResult(Result.Failure(
                Error.Conflict("Tutor.Conflict", ex.Message)));
        }
    }
}
