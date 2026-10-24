using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Tutors.GetTutorById;

public record GetTutorByIdQuery(Guid Id) : IQuery<Result<GetTutorByIdResponse>>;
