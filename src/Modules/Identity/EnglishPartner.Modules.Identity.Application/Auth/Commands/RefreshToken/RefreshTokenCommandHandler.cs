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
using RefreshTokenEntity = EnglishPartner.Modules.Identity.Domain.Users.Entities.RefreshToken;

namespace EnglishPartner.Modules.Identity.Application.Auth.Commands.RefreshToken;

internal sealed class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    ITokenService tokenService,
    ISessionService sessionService,
    IHttpContextAccessor httpContextAccessor,
    IOptions<JwtSettings> jwtSettings) : ICommandHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Validate refresh token from DB
        var existingToken = await refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (existingToken is null || !existingToken.IsValid)
            return Result.Failure<RefreshTokenResponse>(IdentityErrors.Token.InvalidRefreshToken);

        existingToken.Revoke();

        var user = await userRepository.GetByIdAsync(existingToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
            return Result.Failure<RefreshTokenResponse>(IdentityErrors.User.Deactivated);

        // Load fresh roles + permissions from DB (picks up any permission changes since last login)
        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var (roleNames, permissions) = await roleRepository.GetRolesAndPermissionsAsync(roleIds, cancellationToken);

        // Delete the old session — old JWT with old sid is now invalid
        var oldSessionId = httpContextAccessor.HttpContext?.User.FindFirstValue("sid");
        if (oldSessionId is not null)
            await sessionService.DeleteSessionAsync(oldSessionId, cancellationToken);

        // Create new session with fresh TTL
        var sessionTtl = TimeSpan.FromMinutes(jwtSettings.Value.AccessTokenExpirationMinutes + 2);
        var newSessionId = await sessionService.CreateSessionAsync(user.Id, sessionTtl, cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, roleNames, permissions, newSessionId);

        // Rotate refresh token — issue new, revoke old (already done above)
        var newRefreshToken = RefreshTokenEntity.Create(
            user.Id,
            tokenService.GenerateRefreshToken(),
            DateTime.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpirationDays));
        refreshTokenRepository.Add(newRefreshToken);

        // Set new cookie (replaces old __Secure-ep-refresh cookie)
        var context = httpContextAccessor.HttpContext!;
        context.Response.Cookies.Append("__Secure-ep-refresh", newRefreshToken.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/api/v1/auth",
            Expires = DateTimeOffset.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpirationDays),
        });

        var responsePermissions = permissions.Any() ? permissions.ToArray() : null;

        return new RefreshTokenResponse(accessToken, responsePermissions);
    }
}
