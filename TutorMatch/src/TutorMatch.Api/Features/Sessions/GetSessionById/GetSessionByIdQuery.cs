using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Sessions.GetSessionById;

public record GetSessionByIdQuery(Guid Id) : IQuery<Result<GetSessionByIdResponse>>;
