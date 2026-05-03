using EnglishPartner.Modules.Identity.Application.Abstractions;
using EnglishPartner.Modules.Identity.Domain;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace EnglishPartner.Modules.Identity.Application.Auth.Commands.ChangePassword;

internal sealed class ChangePasswordCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ISessionService sessionService,
    IPasswordHasher passwordHasher,
    IHttpContextAccessor httpContextAccessor) : ICommandHandler<ChangePasswordCommand>
{
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(IdentityErrors.User.NotFoundById(request.UserId));

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return Result.Failure(IdentityErrors.User.InvalidCredentials);

        user.ChangePassword(passwordHasher.Hash(request.NewPassword));
        userRepository.Update(user);

        // Security: revoke all refresh tokens so attacker cannot reuse a leaked token
        await refreshTokenRepository.RevokeAllByUserIdAsync(request.UserId, cancellationToken);

        // Invalidate current JWT immediately by deleting the Redis session
        var sessionId = httpContextAccessor.HttpContext?.User.FindFirstValue("sid");
        if (sessionId is not null)
            await sessionService.DeleteSessionAsync(sessionId, cancellationToken);

        // Clear the refresh token cookie — user must re-login
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
