using EnglishPartner.Modules.Identity.Application.Abstractions;
using EnglishPartner.Modules.Identity.Domain;
using EnglishPartner.Modules.Identity.Domain.Roles.Repositories;
using EnglishPartner.Modules.Identity.Domain.Users;
using EnglishPartner.Modules.Identity.Domain.Users.Entities;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;
using EnglishPartner.SharedKernel.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RefreshTokenEntity = EnglishPartner.Modules.Identity.Domain.Users.Entities.RefreshToken;

namespace EnglishPartner.Modules.Identity.Application.Auth.Commands.Register;

internal sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    IUserProfileRepository profileRepository,
    IStreakRepository streakRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IRoleRepository roleRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    ISessionService sessionService,
    IHttpContextAccessor httpContextAccessor,
    TimeProvider timeProvider,
    IOptions<JwtSettings> jwtSettings) : ICommandHandler<RegisterCommand, RegisterResponse>
{
    public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
            return Result.Failure<RegisterResponse>(IdentityErrors.User.EmailAlreadyExists);

        var passwordHash = passwordHasher.Hash(request.Password);
        var user         = User.Create(request.Email, passwordHash, request.DisplayName);
        var profile      = UserProfile.Create(user.Id);
        var streak       = Streak.Create(user.Id);

        // Capture timezone from FE header immediately at registration.
        if (request.UtcOffsetHours.HasValue)
            profile.UpdateTimezone(request.UtcOffsetHours.Value);

        // Assign Learner role by default
        var learnerRole = await roleRepository.GetByNameAsync("Learner", cancellationToken);
        if (learnerRole is not null)
            user.AssignRole(learnerRole.Id);

        userRepository.Add(user);
        profileRepository.Add(profile);
        streakRepository.Add(streak);

        // Learner has no permissions — session still needed for immediate revocation on logout
        var roleNames   = learnerRole is not null ? new[] { learnerRole.Name } : Array.Empty<string>();
        var permissions = Array.Empty<string>();

        var sessionTtl = TimeSpan.FromDays(jwtSettings.Value.RefreshTokenExpirationDays);
        var sessionId  = await sessionService.CreateSessionAsync(user.Id, sessionTtl, cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, roleNames, permissions, sessionId);
        var utcNow = timeProvider.GetUtcNow();
        var expiresAt = utcNow.UtcDateTime.AddMinutes(jwtSettings.Value.AccessTokenExpirationMinutes);

        // Refresh token in DB + httpOnly cookie
        var refreshToken = RefreshTokenEntity.Create(
            user.Id,
            tokenService.GenerateRefreshToken(),
            utcNow.UtcDateTime.AddDays(jwtSettings.Value.RefreshTokenExpirationDays));
        refreshTokenRepository.Add(refreshToken);

        httpContextAccessor.HttpContext!.Response.Cookies.Append("__Secure-ep-refresh", refreshToken.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/api/v1/auth",
            Expires = utcNow.AddDays(jwtSettings.Value.RefreshTokenExpirationDays),
        });

        return new RegisterResponse(user.Id, user.Email, user.DisplayName, accessToken, expiresAt, user.IsOnboardingCompleted);
    }
}
