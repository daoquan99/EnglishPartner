using EnglishPartner.Modules.Identity.Application.Abstractions;
using EnglishPartner.Modules.Identity.Domain;
using EnglishPartner.Modules.Identity.Domain.Roles.Repositories;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;
using EnglishPartner.SharedKernel.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RefreshTokenEntity = EnglishPartner.Modules.Identity.Domain.Users.Entities.RefreshToken;

namespace EnglishPartner.Modules.Identity.Application.Auth.Commands.Login;

internal sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IRoleRepository roleRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    ISessionService sessionService,
    IHttpContextAccessor httpContextAccessor,
    IOptions<JwtSettings> jwtSettings) : ICommandHandler<LoginCommand, LoginResponse>
{
    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailForAuthAsync(request.Email, cancellationToken);

        if (user is null)
            return Result.Failure<LoginResponse>(IdentityErrors.User.InvalidCredentials);

        if (!user.IsActive)
            return Result.Failure<LoginResponse>(IdentityErrors.User.Deactivated);

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result.Failure<LoginResponse>(IdentityErrors.User.InvalidCredentials);

        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var (roleNames, permissions) = await roleRepository.GetRolesAndPermissionsAsync(roleIds, cancellationToken);

        // Session TTL = AccessToken lifetime + 2 min buffer (so session outlives token for valid refresh)
        var sessionTtl = TimeSpan.FromMinutes(jwtSettings.Value.AccessTokenExpirationMinutes + 2);
        var sessionId = await sessionService.CreateSessionAsync(user.Id, sessionTtl, cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, roleNames, permissions, sessionId);

        // Refresh token stored in DB + set as httpOnly cookie (NOT returned in body)
        var refreshToken = RefreshTokenEntity.Create(
            user.Id,
            tokenService.GenerateRefreshToken(),
            DateTime.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpirationDays));
        refreshTokenRepository.Add(refreshToken);

        SetRefreshTokenCookie(refreshToken.Token, jwtSettings.Value.RefreshTokenExpirationDays);

        // Permissions: null for Learner (omitted in JSON), populated for admin-type users
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
            Path = "/api/v1/auth",          // only sent to auth endpoints — not every request
            Expires = DateTimeOffset.UtcNow.AddDays(expirationDays),
        });
    }
}
