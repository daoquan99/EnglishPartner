using EnglishLiveTutor.Api.Common;

namespace EnglishLiveTutor.Api.Security;

/// <summary>
/// Enforces CSRF header check (X-Requested-With or X-CSRF-Token)
/// when cookie-based auth endpoints (/api/auth/refresh, /api/auth/revoke)
/// are invoked using the HttpOnly refresh cookie.
/// </summary>
public sealed class CsrfHeaderMiddleware
{
    private readonly RequestDelegate _next;

    public CsrfHeaderMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // CSRF verification when relying on the HttpOnly cookie for token refresh/revoke
        if (HttpMethods.IsPost(context.Request.Method) &&
            (context.Request.Path.StartsWithSegments("/api/auth/refresh") ||
             context.Request.Path.StartsWithSegments("/api/auth/revoke")) &&
            context.Request.Cookies.ContainsKey("elt_refresh_token"))
        {
            bool hasCsrfHeader =
                context.Request.Headers.ContainsKey("X-Requested-With") ||
                context.Request.Headers.ContainsKey("X-CSRF-Token");

            if (!hasCsrfHeader)
            {
                await ApiProblem.WriteResponseAsync(
                    context.Response,
                    StatusCodes.Status403Forbidden,
                    "Thiếu CSRF protection header (X-Requested-With hoặc X-CSRF-Token) cho yêu cầu xác thực bằng Cookie.");
                return;
            }
        }

        await _next(context);
    }
}
