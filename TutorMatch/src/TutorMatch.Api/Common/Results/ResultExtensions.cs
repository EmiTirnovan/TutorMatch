using Microsoft.AspNetCore.Http;

namespace TutorMatch.Api.Common.Results;

public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return TypedResults.NoContent();
        }

        return MapError(result.Error);
    }

    public static IResult ToHttpResult<TValue>(
        this Result<TValue> result,
        Func<TValue, IResult>? onSuccess = null)
    {
        if (result.IsSuccess)
        {
            return onSuccess != null ? onSuccess(result.Value) : TypedResults.Ok(result.Value);
        }

        return MapError(result.Error);
    }

    private static IResult MapError(Error error)
    {
        var body = new ErrorResponse(error.Code, error.Description);

        return error.Type switch
        {
            ErrorType.NotFound => TypedResults.NotFound(body),
            ErrorType.Conflict => TypedResults.Conflict(body),
            ErrorType.Failure => TypedResults.Json(body, statusCode: StatusCodes.Status500InternalServerError),
            _ => TypedResults.BadRequest(body)
        };
    }
}
