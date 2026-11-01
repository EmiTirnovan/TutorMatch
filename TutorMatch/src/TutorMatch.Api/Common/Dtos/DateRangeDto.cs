namespace TutorMatch.Api.Common.Dtos;

// DTOs and messages contain no business logic and are excluded from unit testing.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record DateRangeDto(DateTime Start, DateTime End);
