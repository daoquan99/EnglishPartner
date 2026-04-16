using EnglishPartner.Modules.Identity.Application.Abstractions;
using EnglishPartner.Modules.Identity.Application.Auth.Commands.Login;
using EnglishPartner.Modules.Identity.Domain;
using EnglishPartner.Modules.Identity.Domain.Roles.Repositories;
using EnglishPartner.Modules.Identity.Domain.Users;
using EnglishPartner.Modules.Identity.Domain.Users.Entities;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;
using EnglishPartner.SharedKernel.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RefreshTokenEntity = EnglishPartner.Modules.Identity.Domain.Users.Entities.RefreshToken;

namespace EnglishPartner.Modules.Identity.Application.Auth.Commands.SocialLogin;

internal sealed class SocialLoginCommandHandler(
    ISocialAuthService socialAuthService,
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IRoleRepository roleRepository,
    ITokenService tokenService,
    ISessionService sessionService,
    IHttpContextAccessor httpContextAccessor,
    IOptions<JwtSettings> jwtSettings,
    ILogger<SocialLoginCommandHandler> logger) : ICommandHandler<SocialLoginCommand, LoginResponse>
{
    public async Task<Result<LoginResponse>> Handle(SocialLoginCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify token with provider (e.g. Google)
        var socialUser = await socialAuthService.VerifyTokenAsync(request.Provider, request.Token, cancellationToken);
        if (socialUser is null)
            return Result.Failure<LoginResponse>(IdentityErrors.Auth.InvalidSocialToken);

        // 2. Find user by ExternalLogin exact match
        var user = await userRepository.GetByExternalLoginAsync(socialUser.Provider, socialUser.ProviderUserId, cancellationToken);

        if (user is null)
        {
            // 3. Fallback: link existing account by email
            user = await userRepository.GetByEmailAsync(socialUser.Email, cancellationToken);

            if (user is null)
            {
                logger.LogInformation("Creating new user for {Email} via {Provider}", socialUser.Email, socialUser.Provider);

                var displayName = !string.IsNullOrWhiteSpace(socialUser.FirstName)
                    ? $"{socialUser.FirstName} {socialUser.LastName}".Trim()
                    : socialUser.Email.Split('@')[0];

                user = User.Create(socialUser.Email, Guid.NewGuid().ToString("N"), displayName);

                var learnerRole = await roleRepository.GetByNameAsync("Learner", cancellationToken);
                if (learnerRole is not null)
                    user.AssignRole(learnerRole.Id);

                userRepository.Add(user);
            }
            else
            {
                logger.LogInformation("Linking existing user {Email} with {Provider}", user.Email, socialUser.Provider);
                userRepository.Update(user);
            }

            user.AddExternalLogin(socialUser.Provider, socialUser.ProviderUserId, socialUser.Provider);
        }

        // 4. Load roles + permissions — single DB call
        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var (roleNames, permissions) = await roleRepository.GetRolesAndPermissionsAsync(roleIds, cancellationToken);

        // 5. Create Redis session — TTL = AccessToken lifetime + 2 min buffer
        var sessionTtl = TimeSpan.FromMinutes(jwtSettings.Value.AccessTokenExpirationMinutes + 2);
        var sessionId = await sessionService.CreateSessionAsync(user.Id, sessionTtl, cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, roleNames, permissions, sessionId);

        // 6. Refresh token in DB + httpOnly cookie (NOT in response body)
        var refreshToken = RefreshTokenEntity.Create(
            user.Id,
            tokenService.GenerateRefreshToken(),
            DateTime.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpirationDays));
        refreshTokenRepository.Add(refreshToken);
        SetRefreshTokenCookie(refreshToken.Token, jwtSettings.Value.RefreshTokenExpirationDays);

        // NOTE: SaveChangesAsync is handled by IdentityUnitOfWorkBehavior — do NOT call it here.

        var responsePermissions = permissions.Any() ? permissions.ToArray() : null;

        return new LoginResponse(
            user.Id,
            user.DisplayName,
            accessToken,
            user.IsOnboardingCompleted,
            responsePermissions);
    }

    private void SetRefreshTokenCookie(string refreshToken, int expirationDays)
    {
        var context = httpContextAccessor.HttpContext!;
        context.Response.Cookies.Append("__Secure-ep-refresh", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/api/v1/auth",
            Expires = DateTimeOffset.UtcNow.AddDays(expirationDays),
        });
    }
}
