using TutorMatch.Api.Features.Tutors.CreateTutor;

namespace TutorMatch.Api.Features.Tutors.GetTutorById;

// API response DTOs are not unit-tested and are excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record GetTutorByIdResponse(
    Guid Id,
    string Name,
    string Email,
    string Bio,
    decimal HourlyRate,
    List<Guid> SubjectIds,
    List<DateRangeDto> Availability);