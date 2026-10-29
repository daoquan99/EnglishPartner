using System.Diagnostics;
using EnglishLiveTutor.Api.Common;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Validation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EnglishLiveTutor.Api.Exceptions;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
        {
            _logger.LogWarning("The response has already started, cannot execute global exception handler.");
            return false;
        }

        // OperationCanceledException when client canceled request
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Request aborted by the client: {Path}", httpContext.Request.Path);
            httpContext.Response.StatusCode = 499;
            return true;
        }

        int statusCode;
        string detail;
        IDictionary<string, string[]>? validationErrors = null;
        IDictionary<string, object?>? customExtensions = null;

        switch (exception)
        {
            case ValidationException appValEx:
                statusCode = StatusCodes.Status400BadRequest;
                detail = appValEx.Message;
                validationErrors = appValEx.Errors
                    .GroupBy(f => f.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());
                _logger.LogWarning(appValEx, "Validation failure on {Path}: {Message}", httpContext.Request.Path, appValEx.Message);
                break;

            case FluentValidation.ValidationException fluentValEx:
                statusCode = StatusCodes.Status400BadRequest;
                detail = fluentValEx.Message;
                validationErrors = fluentValEx.Errors
                    .GroupBy(f => f.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());
                _logger.LogWarning(fluentValEx, "FluentValidation failure on {Path}: {Message}", httpContext.Request.Path, fluentValEx.Message);
                break;

            case NotFoundException notFoundEx:
                statusCode = StatusCodes.Status404NotFound;
                detail = notFoundEx.Message;
                _logger.LogInformation(notFoundEx, "Resource not found on {Path}: {Message}", httpContext.Request.Path, notFoundEx.Message);
                break;

            case ForbiddenException forbiddenEx:
                statusCode = StatusCodes.Status403Forbidden;
                detail = forbiddenEx.Message;
                _logger.LogWarning(forbiddenEx, "Forbidden on {Path}: {Message}", httpContext.Request.Path, forbiddenEx.Message);
                break;

            case ConflictException conflictEx:
                statusCode = StatusCodes.Status409Conflict;
                detail = conflictEx.Message;
                _logger.LogWarning(conflictEx, "Conflict on {Path}: {Message}", httpContext.Request.Path, conflictEx.Message);
                break;

            case UnprocessableEntityException unprocEx:
                statusCode = StatusCodes.Status422UnprocessableEntity;
                detail = unprocEx.Message;
                customExtensions = unprocEx.Extensions;
                _logger.LogWarning(unprocEx, "Unprocessable entity on {Path}: {Message}", httpContext.Request.Path, unprocEx.Message);
                break;

            case TooManyRequestsException tmrEx:
                statusCode = StatusCodes.Status429TooManyRequests;
                detail = tmrEx.Message;
                _logger.LogWarning(tmrEx, "Too many requests on {Path}: {Message}", httpContext.Request.Path, tmrEx.Message);
                break;

            case UnauthorizedAccessException unauthEx:
                statusCode = StatusCodes.Status401Unauthorized;
                detail = string.IsNullOrWhiteSpace(unauthEx.Message) ? "Bạn cần đăng nhập để thực hiện thao tác này." : unauthEx.Message;
                _logger.LogWarning(unauthEx, "Unauthorized access on {Path}: {Message}", httpContext.Request.Path, unauthEx.Message);
                break;

            case BadHttpRequestException badRequestEx:
                statusCode = badRequestEx.StatusCode;
                detail = "Yêu cầu không hợp lệ.";
                _logger.LogWarning("Bad HTTP request on {Path}: {ExceptionType}", httpContext.Request.Path, badRequestEx.GetType().Name);
                break;

            case System.Text.Json.JsonException:
                // Client-supplied JSON (lesson import, answer payloads) that fails to parse is a bad request, not a server fault.
                statusCode = StatusCodes.Status400BadRequest;
                detail = "Dữ liệu JSON không hợp lệ.";
                _logger.LogWarning("Invalid JSON payload on {Path}", httpContext.Request.Path);
                break;

            case NotSupportedException notSupportedEx:
                statusCode = StatusCodes.Status400BadRequest;
                detail = notSupportedEx.Message;
                _logger.LogWarning("Unsupported operation on {Path}: {Message}", httpContext.Request.Path, notSupportedEx.Message);
                break;

            case InvalidOperationException invalidOpEx:
                statusCode = StatusCodes.Status400BadRequest;
                detail = invalidOpEx.Message;
                _logger.LogWarning(invalidOpEx, "Business rule violation on {Path}: {Message}", httpContext.Request.Path, invalidOpEx.Message);
                break;

            case ArgumentException argEx:
                statusCode = StatusCodes.Status400BadRequest;
                detail = argEx.Message;
                _logger.LogWarning(argEx, "Invalid argument on {Path}: {Message}", httpContext.Request.Path, argEx.Message);
                break;

            default:
                statusCode = StatusCodes.Status500InternalServerError;
                detail = ApiProblem.DefaultUnexpectedErrorMessage;
                string traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
                _logger.LogError(exception, "Unhandled server error on {Path}. TraceId: {TraceId}", httpContext.Request.Path, traceId);
                break;
        }

        await ApiProblem.WriteResponseAsync(
            httpContext.Response,
            statusCode,
            detail,
            errors: validationErrors,
            customExtensions: customExtensions,
            cancellationToken: cancellationToken);

        return true;
    }
}
