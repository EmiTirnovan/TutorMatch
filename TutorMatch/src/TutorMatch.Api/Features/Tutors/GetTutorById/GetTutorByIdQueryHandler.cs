using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Dtos;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Tutors;

namespace TutorMatch.Api.Features.Tutors.GetTutorById;

public class GetTutorByIdQueryHandler : IQueryHandler<GetTutorByIdQuery, Result<GetTutorByIdResponse>>
{
    private readonly ITutorRepository _repository;

    public GetTutorByIdQueryHandler(ITutorRepository repository)
    {
        _repository = repository;
    }

    public Task<Result<GetTutorByIdResponse>> HandleAsync(GetTutorByIdQuery query, CancellationToken cancellationToken = default)
    {
        var tutor = _repository.GetById(query.Id);
        if (tutor is null)
        {
            return Task.FromResult(Result<GetTutorByIdResponse>.Failure(
                Error.NotFound("Tutor.NotFound", $"Tutor with ID '{query.Id}' was not found.")));
        }

        var subjects = tutor.TutorSubjects
            .Select(ts => ts.Subject?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToList();

        var response = new GetTutorByIdResponse(
            tutor.Id,
            tutor.Name,
            tutor.Email,
            tutor.Bio,
            tutor.HourlyRate,
            tutor.TutorSubjects.Select(ts => ts.SubjectId).ToList(),
            tutor.Availability.Select(a => new DateRangeDto(a.Start, a.End)).ToList(),
            subjects
        );

        return Task.FromResult(Result<GetTutorByIdResponse>.Success(response));
    }
}
