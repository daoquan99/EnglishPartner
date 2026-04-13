using EnglishPartner.Modules.Identity.Application.Abstractions;
using EnglishPartner.Modules.Identity.Application.Auth.Commands.Login;
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
    ITokenService tokenService,
    IIdentityUnitOfWork unitOfWork,
    IOptions<JwtSettings> jwtSettings,
    ILogger<SocialLoginCommandHandler> logger) : ICommandHandler<SocialLoginCommand, LoginResponse>
{
    public async Task<Result<LoginResponse>> Handle(SocialLoginCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify token with provider (e.g. Google)
        var socialUser = await socialAuthService.VerifyTokenAsync(request.Provider, request.Token, cancellationToken);
        
        if (socialUser is null)
        {
            return Result.Failure<LoginResponse>(new Error("Auth.SocialLogin.InvalidToken", "Invalid or expired social token."));
        }

        // 2. Find user by ExternalLogin exact match
        var user = await userRepository.GetByExternalLoginAsync(socialUser.Provider, socialUser.ProviderUserId, cancellationToken);

        if (user is null)
        {
            // 3. Fallback: Search by Email to link existing account
            user = await userRepository.GetByEmailAsync(socialUser.Email, cancellationToken);

            if (user is null)
            {
                logger.LogInformation("Creating new auto-registered user for {Email} via {Provider}", socialUser.Email, socialUser.Provider);
                
                var placeholderHash = Guid.NewGuid().ToString("N");
                var displayName = !string.IsNullOrWhiteSpace(socialUser.FirstName) 
                    ? $"{socialUser.FirstName} {socialUser.LastName}".Trim() 
                    : socialUser.Email.Split('@')[0];

                user = User.Create(socialUser.Email, placeholderHash, displayName);
                userRepository.Add(user);
            }
            else
            {
                logger.LogInformation("Linking existing user {Email} with new provider {Provider}", user.Email, socialUser.Provider);
                // The Update will be handled by UnitOfWork save, but we must ensure EF tracks the change.
                userRepository.Update(user); // Mostly to ensure tracking, although usually EF tracks already retrieved entities.
            }

            // Record the external login map
            user.AddExternalLogin(socialUser.Provider, socialUser.ProviderUserId, socialUser.Provider);
        }

        // 4. Generate JWT & Refresh Tokens
        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, [], []);
        var refreshTokenStr = tokenService.GenerateRefreshToken();

        var refreshToken = RefreshTokenEntity.Create(
            user.Id, 
            refreshTokenStr, 
            DateTime.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpirationDays));
            
        refreshTokenRepository.Add(refreshToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResponse(
            user.Id,
            user.DisplayName,
            accessToken,
            refreshTokenStr,
            user.IsOnboardingCompleted);
    }
}
