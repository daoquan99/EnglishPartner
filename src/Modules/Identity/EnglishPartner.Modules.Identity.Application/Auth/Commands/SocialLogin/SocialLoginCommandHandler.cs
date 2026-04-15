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

                // Assign Learner role to new social users
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

        // 4. Load roles + permissions for JWT — single DB call (was 2 sequential queries)
        // (IdentityUnitOfWorkBehavior will SaveChanges after this handler returns)
        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var (roleNames, permissions) = await roleRepository.GetRolesAndPermissionsAsync(roleIds, cancellationToken);

        var accessToken    = tokenService.GenerateAccessToken(user.Id, user.Email, roleNames, permissions);

        var refreshTokenStr = tokenService.GenerateRefreshToken();

        var refreshToken = RefreshTokenEntity.Create(
            user.Id,
            refreshTokenStr,
            DateTime.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpirationDays));

        refreshTokenRepository.Add(refreshToken);

        // NOTE: SaveChangesAsync is handled by IdentityUnitOfWorkBehavior — do NOT call it here.

        return new LoginResponse(user.Id, user.DisplayName, accessToken, refreshTokenStr, user.IsOnboardingCompleted);
    }
}
