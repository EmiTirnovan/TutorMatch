using TutorMatch.Api.Features.Tutors.CreateTutor;

namespace TutorMatch.Api.Features.Tutors.GetTutorById;

public record GetTutorByIdResponse(
    Guid Id,
    string Name,
    string Email,
    string Bio,
    decimal HourlyRate,
    List<Guid> SubjectIds,
    List<DateRangeDto> Availability);