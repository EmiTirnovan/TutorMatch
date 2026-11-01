using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Sessions;

namespace TutorMatch.Api.Features.Sessions.GetMySessions;

// DTOs and messages contain no business logic and are excluded from unit testing.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public record GetMySessionsQuery(Guid UserId, string Role, SessionStatus? Status = null) : IQuery<Result<GetMySessionsResponse>>;

