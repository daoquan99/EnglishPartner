using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace EnglishPartner.SharedKernel.Extensions;

/// <summary>
/// Extension methods for HttpContext / ClaimsPrincipal to extract common JWT claims.
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>Attempts to parse the current user's ID from the JWT NameIdentifier claim.</summary>
    public static bool TryGetUserId(this HttpContext ctx, out Guid userId)
        => ctx.User.TryGetUserId(out userId);

    /// <summary>Attempts to parse the user's ID from a ClaimsPrincipal (for endpoints using ClaimsPrincipal injection).</summary>
    public static bool TryGetUserId(this ClaimsPrincipal user, out Guid userId)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId);
    }

    /// <summary>Gets the current user's ID from HttpContext. Throws if claim is missing.</summary>
    public static Guid GetUserId(this HttpContext ctx)
    {
        if (!ctx.TryGetUserId(out var userId))
            throw new InvalidOperationException("User ID claim is missing or invalid.");
        return userId;
    }
}
