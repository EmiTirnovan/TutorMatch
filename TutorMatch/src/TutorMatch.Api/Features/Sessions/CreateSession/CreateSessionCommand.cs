using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Sessions.CreateSession;

// DTOs and messages contain no business logic and are excluded from unit testing.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record CreateSessionCommand(
    Guid TutorId,
    Guid StudentId,
    DateTime StartTime,
    DateTime EndTime
) : ICommand<Result<CreateSessionResponse>>;
