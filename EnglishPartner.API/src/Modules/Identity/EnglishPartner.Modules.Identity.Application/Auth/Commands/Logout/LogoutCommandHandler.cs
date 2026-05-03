using EnglishPartner.Modules.Identity.Application.Abstractions;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace EnglishPartner.Modules.Identity.Application.Auth.Commands.Logout;

internal sealed class LogoutCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    ISessionService sessionService,
    IHttpContextAccessor httpContextAccessor) : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // 1. Revoke all refresh tokens from DB
        await refreshTokenRepository.RevokeAllByUserIdAsync(request.UserId, cancellationToken);

        // 2. Delete session from Redis — AccessToken is immediately invalid (even if not expired)
        var sessionId = httpContextAccessor.HttpContext?.User.FindFirstValue("sid");
        if (sessionId is not null)
            await sessionService.DeleteSessionAsync(sessionId, cancellationToken);

        // 3. Clear the httpOnly refresh token cookie
        httpContextAccessor.HttpContext?.Response.Cookies.Delete("__Secure-ep-refresh", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/api/v1/auth",
        });

        return Result.Success();
    }
}
