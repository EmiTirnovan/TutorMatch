namespace TutorMatch.Api.Features.Tutors.CreateTutor;

public record DateRangeDto(DateTime Start, DateTime End);

public record CreateTutorRequest(
    string Name,
    string Email,
    string Bio,
    decimal HourlyRate,
    List<Guid>? SubjectIds,
    List<DateRangeDto>? Availability);