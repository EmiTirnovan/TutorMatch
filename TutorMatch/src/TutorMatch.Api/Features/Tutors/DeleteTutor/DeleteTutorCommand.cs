using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Tutors.DeleteTutor;

// DTOs and messages contain no business logic and are excluded from unit testing.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record DeleteTutorCommand(Guid Id) : ICommand;
