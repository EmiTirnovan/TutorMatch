namespace TutorMatch.Api.Features.Tutors.GetTutors;

public record TutorSummaryResponse(Guid Id, string Name, string Email, decimal HourlyRate);