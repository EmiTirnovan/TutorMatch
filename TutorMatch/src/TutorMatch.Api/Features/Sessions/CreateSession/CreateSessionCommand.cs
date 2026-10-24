using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Sessions.CreateSession;

public record CreateSessionCommand(
    Guid TutorId,
    Guid StudentId,
    DateTime StartTime,
    DateTime EndTime
) : ICommand<Result<CreateSessionResponse>>;
