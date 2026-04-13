using System.Security.Claims;
using EnglishPartner.SharedKernel.Interfaces;

namespace EnglishPartner.Api.Services;

/// <summary>
/// Resolves current user context from HttpContext claims.
/// </summary>
internal sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email
        => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated
        => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public bool IsAdmin
        => httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;
}
