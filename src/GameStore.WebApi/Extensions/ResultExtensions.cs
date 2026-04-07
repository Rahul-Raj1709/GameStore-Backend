using GameStore.Domain.Shared;

namespace GameStore.WebApi.Extensions;

public static class ResultExtensions
{
    public static IResult ToProblemDetails(this Result result)
    {
        if (result.IsSuccess) throw new InvalidOperationException("Cannot convert success result to problem details.");

        var statusCode = result.Error.Code switch
        {
            "Game.NotFound" => StatusCodes.Status404NotFound,
            "Auth.InvalidCredentials" => StatusCodes.Status401Unauthorized,
            "Auth.EmailAlreadyExists" => StatusCodes.Status409Conflict,
            "Auth.Forbidden" => StatusCodes.Status403Forbidden, // <-- NEW
            "Error.ConditionNotMet" => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(statusCode: statusCode, title: result.Error.Code, detail: result.Error.Description);
    }

    public static IResult Match<T>(this Result<T> result, Func<T, IResult> onSuccess)
    {
        return result.IsSuccess ? onSuccess(result.Value) : result.ToProblemDetails();
    }

    public static IResult Match(this Result result, Func<IResult> onSuccess)
    {
        return result.IsSuccess ? onSuccess() : result.ToProblemDetails();
    }
}