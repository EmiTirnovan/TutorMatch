using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Sessions.DeleteSession;

public record DeleteSessionCommand(Guid Id) : ICommand;
