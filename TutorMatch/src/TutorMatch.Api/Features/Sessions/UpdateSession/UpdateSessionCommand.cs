using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Sessions.UpdateSession;

// DTOs and messages contain no business logic and are excluded from unit testing.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record UpdateSessionCommand(
    Guid Id,
    DateTime StartTime,
    DateTime EndTime
) : ICommand;
