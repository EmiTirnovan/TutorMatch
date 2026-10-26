namespace TutorMatch.Api.Features.Tutors.GetTutors;

// API response DTOs are not unit-tested and are excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record TutorSummaryResponse(Guid Id, string Name, string Email, decimal HourlyRate);