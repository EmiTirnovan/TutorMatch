namespace TutorMatch.Api.Features.Tutors.CreateTutor;

// API request DTOs are not unit-tested and are excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record DateRangeDto(DateTime Start, DateTime End);

// API request DTOs are not unit-tested and are excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record CreateTutorRequest(
    string Name,
    string Email,
    string Bio,
    decimal HourlyRate,
    List<Guid>? SubjectIds,
    List<DateRangeDto>? Availability);