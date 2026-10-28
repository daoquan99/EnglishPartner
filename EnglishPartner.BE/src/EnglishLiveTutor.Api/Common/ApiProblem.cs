using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EnglishLiveTutor.Api.Common;

public static class ApiProblem
{
    public const string DefaultUnexpectedErrorMessage = "Đã xảy ra lỗi không mong muốn trên máy chủ. Vui lòng thử lại sau.";

    public static ProblemDetails Create(
        int statusCode,
        string detail,
        HttpContext? httpContext = null,
        string? title = null,
        string? type = null,
        IDictionary<string, string[]>? errors = null,
        IDictionary<string, object?>? customExtensions = null)
    {
        string defaultTitle = GetDefaultTitle(statusCode);
        string defaultType = GetDefaultType(statusCode);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title ?? defaultTitle,
            Type = type ?? defaultType,
            Detail = detail,
            Instance = httpContext?.Request?.Path.Value
        };

        string traceId = Activity.Current?.Id ?? httpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N");
        problem.Extensions["traceId"] = traceId;
        problem.Extensions["error"] = detail;

        if (errors is not null && errors.Count > 0)
        {
            problem.Extensions["errors"] = errors;
        }

        if (customExtensions is not null)
        {
            foreach (var (key, value) in customExtensions)
            {
                if (key != "traceId" && key != "error" && key != "errors")
                {
                    problem.Extensions[key] = value;
                }
            }
        }

        return problem;
    }

    public static IResult Result(
        int statusCode,
        string detail,
        HttpContext? httpContext = null,
        string? title = null,
        string? type = null,
        IDictionary<string, string[]>? errors = null,
        IDictionary<string, object?>? customExtensions = null)
    {
        var problem = Create(statusCode, detail, httpContext, title, type, errors, customExtensions);
        return Results.Json(problem, statusCode: statusCode, contentType: "application/problem+json");
    }

    public static IResult BadRequest(
        string detail,
        HttpContext? httpContext = null,
        IDictionary<string, string[]>? errors = null,
        IDictionary<string, object?>? customExtensions = null)
        => Result(StatusCodes.Status400BadRequest, detail, httpContext, errors: errors, customExtensions: customExtensions);

    public static IResult Unauthorized(
        string detail = "Bạn cần đăng nhập để thực hiện thao tác này.",
        HttpContext? httpContext = null,
        IDictionary<string, object?>? customExtensions = null)
        => Result(StatusCodes.Status401Unauthorized, detail, httpContext, customExtensions: customExtensions);

    public static IResult Forbidden(
        string detail = "Bạn không có quyền truy cập tài nguyên này.",
        HttpContext? httpContext = null,
        IDictionary<string, object?>? customExtensions = null)
        => Result(StatusCodes.Status403Forbidden, detail, httpContext, customExtensions: customExtensions);

    public static IResult NotFound(
        string detail = "Không tìm thấy tài nguyên yêu cầu.",
        HttpContext? httpContext = null,
        IDictionary<string, object?>? customExtensions = null)
        => Result(StatusCodes.Status404NotFound, detail, httpContext, customExtensions: customExtensions);

    public static IResult Conflict(
        string detail,
        HttpContext? httpContext = null,
        IDictionary<string, object?>? customExtensions = null)
        => Result(StatusCodes.Status409Conflict, detail, httpContext, customExtensions: customExtensions);

    public static async Task WriteResponseAsync(
        HttpResponse response,
        int statusCode,
        string detail,
        string? title = null,
        string? type = null,
        IDictionary<string, string[]>? errors = null,
        IDictionary<string, object?>? customExtensions = null,
        CancellationToken cancellationToken = default)
    {
        var problem = Create(statusCode, detail, response.HttpContext, title, type, errors, customExtensions);
        response.StatusCode = statusCode;
        await response.WriteAsJsonAsync(problem, options: (System.Text.Json.JsonSerializerOptions?)null, contentType: "application/problem+json", cancellationToken: cancellationToken);
    }

    public static Task WriteResponseAsync(
        HttpContext httpContext,
        int statusCode,
        string detail,
        string? title = null,
        string? type = null,
        IDictionary<string, string[]>? errors = null,
        IDictionary<string, object?>? customExtensions = null,
        CancellationToken cancellationToken = default)
        => WriteResponseAsync(httpContext.Response, statusCode, detail, title, type, errors, customExtensions, cancellationToken);

    public static string GetDefaultTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status409Conflict => "Conflict",
        StatusCodes.Status429TooManyRequests => "Too Many Requests",
        StatusCodes.Status500InternalServerError => "Internal Server Error",
        _ => "An error occurred"
    };

    public static string GetDefaultType(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        StatusCodes.Status401Unauthorized => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
        StatusCodes.Status403Forbidden => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
        StatusCodes.Status404NotFound => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        StatusCodes.Status409Conflict => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        StatusCodes.Status429TooManyRequests => "https://tools.ietf.org/html/rfc6585#section-4",
        StatusCodes.Status500InternalServerError => "https://tools.ietf.org/html/rfc9110#section-15.6.1",
        _ => $"https://httpstatuses.io/{statusCode}"
    };
}
