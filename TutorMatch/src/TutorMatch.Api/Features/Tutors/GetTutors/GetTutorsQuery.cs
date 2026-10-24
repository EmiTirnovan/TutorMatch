using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;

namespace TutorMatch.Api.Features.Tutors.GetTutors;

public record GetTutorsQuery : IQuery<Result<List<TutorSummaryResponse>>>;
