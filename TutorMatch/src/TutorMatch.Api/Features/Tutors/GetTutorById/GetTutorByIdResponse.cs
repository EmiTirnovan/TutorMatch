using TutorMatch.Api.Common.Dtos;

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
    List<DateRangeDto> Availability,
    List<string>? Subjects = null,
    double AverageRating = 0,
    int RatingsCount = 0,
    List<TutorReviewDto>? Reviews = null,
    Dictionary<int, int>? RatingDistribution = null);

// Date is the start of the rated session (falls back to the rating date if the session no longer exists).
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record TutorReviewDto(
    Guid SessionId,
    DateTime Date,
    int Score,
    string? Comment,
    string? StudentName);
