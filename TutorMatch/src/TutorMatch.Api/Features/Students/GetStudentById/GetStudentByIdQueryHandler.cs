using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Students;

namespace TutorMatch.Api.Features.Students.GetStudentById;

public class GetStudentByIdQueryHandler : IQueryHandler<GetStudentByIdQuery, Result<GetStudentByIdResponse>>
{
    private readonly IStudentRepository _repository;

    public GetStudentByIdQueryHandler(IStudentRepository repository)
    {
        _repository = repository;
    }

    public Task<Result<GetStudentByIdResponse>> HandleAsync(GetStudentByIdQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Id == Guid.Empty)
        {
            return Task.FromResult(Result<GetStudentByIdResponse>.Failure(
                Error.Validation("Student.Validation", "Student ID cannot be empty.")));
        }

        var student = _repository.GetById(query.Id);
        if (student is null)
        {
            return Task.FromResult(Result<GetStudentByIdResponse>.Failure(
                Error.NotFound("Student.NotFound", $"Student with ID '{query.Id}' was not found.")));
        }

        var response = new GetStudentByIdResponse(student.Id, student.Name, student.Email);
        return Task.FromResult(Result<GetStudentByIdResponse>.Success(response));
    }
}
