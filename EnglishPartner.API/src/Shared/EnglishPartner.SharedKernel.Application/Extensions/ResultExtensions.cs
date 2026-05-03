using Microsoft.AspNetCore.Http;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.SharedKernel.Extensions;

public static class ResultExtensions
{
    public static IResult ToProblemDetails(this Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("Cannot convert success result to problem details.");

        return result.Error is ValidationError validationError
            ? Microsoft.AspNetCore.Http.Results.ValidationProblem(
                validationError.Errors
                    .GroupBy(e => e.Code)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray()),
                title: "Validation Failed",
                statusCode: StatusCodes.Status400BadRequest)
            : Microsoft.AspNetCore.Http.Results.Problem(
                title: result.Error.Code,
                detail: result.Error.Message,
                statusCode: StatusCodes.Status400BadRequest);
    }
    public static IResult ToProblemDetails<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("Cannot convert success result to problem details.");

        return ((Result)result).ToProblemDetails();
    }
}
