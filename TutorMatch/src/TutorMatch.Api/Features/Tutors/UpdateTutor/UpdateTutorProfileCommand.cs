using TutorMatch.Api.Common.Cqrs;

namespace TutorMatch.Api.Features.Tutors.UpdateTutor;

// DTOs and messages contain no business logic and are excluded from unit testing.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record UpdateTutorProfileCommand(
    Guid Id,
    string Name,
    string Bio,
    decimal HourlyRate,
    List<string>? Subjects = null
) : ICommand;
