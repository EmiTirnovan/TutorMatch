using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Tutors.CreateTutor;

// DTOs and messages contain no business logic and are excluded from unit testing.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record CreateTutorCommand(
    string Name,
    string Email,
    string Bio,
    decimal HourlyRate,
    List<Guid>? SubjectIds = null,
    List<DateRangeDto>? Availability = null
) : ICommand<Result<CreateTutorResponse>>;
