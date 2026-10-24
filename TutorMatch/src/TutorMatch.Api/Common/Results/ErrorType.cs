namespace TutorMatch.Api.Common.Results;

public enum ErrorType
{
    Validation = 400,
    NotFound = 404,
    Conflict = 409,
    Failure = 500
}
