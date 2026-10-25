namespace TutorMatch.Api.Features.Tutors.CreateTutor;

// API response DTOs are not unit-tested and are excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record CreateTutorResponse(
    Guid Id,
    string Name,
    string Email,
    string Bio,
    decimal HourlyRate);