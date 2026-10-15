using EnglishLiveTutor.Application.Common.Interfaces;

namespace EnglishLiveTutor.Api.Security;

/// <summary>
/// Middleware that:
/// 1. Extracts Bearer access token from Authorization header (or access_token query for SignalR hubs)
///    and populates HttpContext.User with verified role & permission claims.
/// 2. Enforces CSRF header check (X-Requested-With or X-CSRF-Token) when cookie-based auth endpoints
///    (/api/auth/refresh, /api/auth/revoke) are invoked using the HttpOnly refresh cookie.
/// </summary>
public sealed class JwtAndCsrfMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IJwtTokenService _jwtTokenService;

    public JwtAndCsrfMiddleware(RequestDelegate next, IJwtTokenService jwtTokenService)
    {
        _next = next;
        _jwtTokenService = jwtTokenService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string? token = null;
        string? authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(authHeader) &&
            authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = authHeader["Bearer ".Length..].Trim();
        }
        else if (context.Request.Query.TryGetValue("access_token", out var queryToken))
        {
            token = queryToken.FirstOrDefault();
        }

        if (!string.IsNullOrWhiteSpace(token))
        {
            var principal = _jwtTokenService.ValidateAccessToken(token);
            if (principal is not null)
            {
                context.User = principal;
            }
        }

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
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Thiếu CSRF protection header (X-Requested-With hoặc X-CSRF-Token) cho yêu cầu xác thực bằng Cookie."
                });
                return;
            }
        }

        await _next(context);
    }
}
