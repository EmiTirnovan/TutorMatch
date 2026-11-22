using TutorMatch.Api.Common.Cqrs;

namespace TutorMatch.Api.Features.Students.UpdateProfile;

// DTOs and messages contain no business logic and are excluded from unit testing.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record UpdateStudentProfileCommand(Guid Id, string Name) : ICommand;
