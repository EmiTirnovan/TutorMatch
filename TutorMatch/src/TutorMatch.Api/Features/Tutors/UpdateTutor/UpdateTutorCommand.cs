using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Tutors.UpdateTutor;

public record UpdateTutorCommand(
    Guid Id,
    string Name,
    string Bio,
    decimal HourlyRate
) : ICommand;
