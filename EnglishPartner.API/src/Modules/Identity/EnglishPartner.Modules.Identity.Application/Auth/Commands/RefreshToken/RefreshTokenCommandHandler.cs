using EnglishPartner.Modules.Identity.Application.Abstractions;
using EnglishPartner.Modules.Identity.Domain;
using EnglishPartner.Modules.Identity.Domain.Roles.Repositories;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;
using EnglishPartner.SharedKernel.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Security.Claims; 

namespace EnglishPartner.Modules.Identity.Application.Auth.Commands.RefreshToken;

internal sealed class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    ITokenService tokenService,
    ISessionService sessionService,
    IHttpContextAccessor httpContextAccessor,
    TimeProvider timeProvider,
    IOptions<JwtSettings> jwtSettings) : ICommandHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var existingToken = await refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        if (existingToken is null || !existingToken.IsValid(utcNow))
            return Result.Failure<RefreshTokenResponse>(IdentityErrors.Token.InvalidRefreshToken);

        var user = await userRepository.GetByIdAsync(existingToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
            return Result.Failure<RefreshTokenResponse>(IdentityErrors.User.Deactivated);

        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var (roleNames, permissions) = await roleRepository.GetRolesAndPermissionsAsync(roleIds, cancellationToken);

        var oldSessionId = httpContextAccessor.HttpContext?.User.FindFirstValue("sid");
        if (oldSessionId is not null)
            await sessionService.DeleteSessionAsync(oldSessionId, cancellationToken);

        var sessionTtl = TimeSpan.FromDays(jwtSettings.Value.RefreshTokenExpirationDays);
        var newSessionId = await sessionService.CreateSessionAsync(user.Id, sessionTtl, cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, roleNames, permissions, newSessionId);
        var expiresAt = timeProvider.GetUtcNow().UtcDateTime.AddMinutes(jwtSettings.Value.AccessTokenExpirationMinutes);

        // Rotate refresh token in-place (UPDATE existing row, not INSERT new)
        // Old token value is replaced immediately — any reuse of old token will fail IsValid()
        var newRawToken = tokenService.GenerateRefreshToken();
        var newExpiry = timeProvider.GetUtcNow().UtcDateTime.AddDays(jwtSettings.Value.RefreshTokenExpirationDays);
        existingToken.Rotate(newRawToken, newExpiry);

        // Update cookie with new token value
        httpContextAccessor.HttpContext!.Response.Cookies.Append("__Secure-ep-refresh", newRawToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/api/v1/auth",
            Expires = timeProvider.GetUtcNow().AddDays(jwtSettings.Value.RefreshTokenExpirationDays),
        });

        var responsePermissions = permissions.Any() ? permissions.ToArray() : null;

        return new RefreshTokenResponse(accessToken, expiresAt, responsePermissions);
    }
}
