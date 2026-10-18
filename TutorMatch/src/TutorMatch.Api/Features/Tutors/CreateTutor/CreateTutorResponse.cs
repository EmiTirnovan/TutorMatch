namespace TutorMatch.Api.Features.Tutors.CreateTutor;

public record CreateTutorResponse(
    Guid Id,
    string Name,
    string Email,
    string Bio,
    decimal HourlyRate);