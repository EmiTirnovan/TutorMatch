namespace TutorMatch.Api.Features.Tutors.UpdateTutor;

// API request DTOs are not unit-tested and are excluded from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record UpdateTutorRequest(string Name, string Bio, decimal HourlyRate);