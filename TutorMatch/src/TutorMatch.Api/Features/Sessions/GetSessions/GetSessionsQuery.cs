using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Sessions.GetSessions;

public record GetSessionsQuery : IQuery<Result<List<GetSessionsResponse>>>;
