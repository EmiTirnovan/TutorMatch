using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Tutors;

namespace TutorMatch.Api.Features.Tutors.GetTutors;

public class GetTutorsQueryHandler : IQueryHandler<GetTutorsQuery, Result<List<TutorSummaryResponse>>>
{
    private readonly ITutorRepository _repository;

    public GetTutorsQueryHandler(ITutorRepository repository)
    {
        _repository = repository;
    }

    public Task<Result<List<TutorSummaryResponse>>> HandleAsync(GetTutorsQuery query, CancellationToken cancellationToken = default)
    {
        var tutors = _repository.GetAll();

        var response = tutors.Select(t => new TutorSummaryResponse(
            t.Id,
            t.Name,
            t.Email,
            t.HourlyRate
        )).ToList();

        return Task.FromResult(Result<List<TutorSummaryResponse>>.Success(response));
    }
}
