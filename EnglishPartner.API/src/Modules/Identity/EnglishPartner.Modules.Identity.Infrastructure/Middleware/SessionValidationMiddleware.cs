using EnglishPartner.Modules.Identity.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace EnglishPartner.Modules.Identity.Infrastructure.Middleware;

/// <summary>
/// Validates that the session ID embedded in the JWT still exists in Redis.
/// This makes JWT revocation possible:
///   - Logout deletes the session from Redis
///   - Even if the JWT hasn't expired yet, requests with that JWT return 401
/// Only runs for authenticated requests (requests with a valid JWT signature).
/// </summary>
public sealed class SessionValidationMiddleware(RequestDelegate next, ILogger<SessionValidationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Only validate authenticated users (skip anonymous endpoints)
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var sessionId = context.User.FindFirstValue("sid");

            if (string.IsNullOrEmpty(sessionId))
            {
                // JWT was issued before session tracking was added — reject for security
                logger.LogWarning("Authenticated request missing 'sid' claim. Rejecting.");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { title = "Token invalid", status = 401 });
                return;
            }

            var sessionService = context.RequestServices.GetRequiredService<ISessionService>();
            var isValid = await sessionService.ValidateSessionAsync(sessionId, context.RequestAborted);

            if (!isValid)
            {
                logger.LogInformation("Session {SessionId} not found in Redis — token revoked or expired.", sessionId);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { title = "Session expired or revoked", status = 401 });
                return;
            }
        }

        await next(context);
    }
}
