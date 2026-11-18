using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Features.Sessions.BookSession;

// DTOs and messages contain no business logic and are excluded from unit testing.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record BookSessionCommand(
    Guid TutorId,
    Guid StudentId,
    DateTime StartTime,
    DateTime EndTime,
    string? Subject = null,
    string? Notes = null,
    SessionType Type = SessionType.Online,
    string? LocationName = null,
    double? Latitude = null,
    double? Longitude = null
) : ICommand<Result<BookSessionResponse>>;
