using TutorMatch.Api.Common.Dtos;

namespace TutorMatch.Api.Features.Tutors.CreateTutor;

// DTOs and messages contain no business logic and are excluded from unit testing.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record CreateTutorRequest(
    string Name,
    string Email,
    string Bio,
    decimal HourlyRate,
    List<Guid>? SubjectIds = null,
    List<DateRangeDto>? Availability = null,
    List<string>? Subjects = null);
