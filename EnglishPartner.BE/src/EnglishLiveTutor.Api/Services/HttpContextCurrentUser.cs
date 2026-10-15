using System.Security.Claims;
using EnglishLiveTutor.Application.Common.Interfaces;

namespace EnglishLiveTutor.Api.Services;

public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context is null)
            {
                return "system";
            }

            string? claimUserId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User?.FindFirstValue("sub")
                ?? context.Request.Headers["X-User-Id"].FirstOrDefault();

            return string.IsNullOrWhiteSpace(claimUserId) ? "default-learner" : claimUserId.Trim();
        }
    }
}
