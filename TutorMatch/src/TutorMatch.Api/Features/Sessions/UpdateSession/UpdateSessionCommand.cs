using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Sessions.UpdateSession;

public record UpdateSessionCommand(
    Guid Id,
    DateTime StartTime,
    DateTime EndTime
) : ICommand;
