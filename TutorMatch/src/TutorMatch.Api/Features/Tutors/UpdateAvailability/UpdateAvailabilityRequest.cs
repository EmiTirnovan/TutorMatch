namespace TutorMatch.Api.Features.Tutors.UpdateAvailability;

using TutorMatch.Api.Common.Dtos;

// DTOs and messages contain no business logic and are excluded from unit testing.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record UpdateAvailabilityRequest(List<DateRangeDto> Availability);
