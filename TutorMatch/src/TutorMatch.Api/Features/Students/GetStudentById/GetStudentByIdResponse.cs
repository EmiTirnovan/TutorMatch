namespace TutorMatch.Api.Features.Students.GetStudentById;

// API response DTO for student profile details
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record GetStudentByIdResponse(Guid Id, string Name, string Email);
