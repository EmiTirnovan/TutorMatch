using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Sessions.BookSession;

// DTOs and messages contain no business logic and are excluded from unit testing.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record BookSessionCommand(
    Guid TutorId,
    Guid StudentId,
    DateTime StartTime,
    DateTime EndTime
) : ICommand<Result<BookSessionResponse>>;
