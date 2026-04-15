using EnglishPartner.Modules.Identity.Application.Abstractions;
using EnglishPartner.Modules.Identity.Domain;
using EnglishPartner.Modules.Identity.Domain.Roles.Repositories;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;
using EnglishPartner.SharedKernel.Settings;
using Microsoft.Extensions.Options;
using RefreshTokenEntity = EnglishPartner.Modules.Identity.Domain.Users.Entities.RefreshToken;

namespace EnglishPartner.Modules.Identity.Application.Auth.Commands.Login;

internal sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IRoleRepository roleRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IOptions<JwtSettings> jwtSettings) : ICommandHandler<LoginCommand, LoginResponse>
{
    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Lean query: no ExternalLogins join needed for password login path
        var user = await userRepository.GetByEmailForAuthAsync(request.Email, cancellationToken);

        if (user is null)
            return Result.Failure<LoginResponse>(IdentityErrors.User.InvalidCredentials);

        if (!user.IsActive)
            return Result.Failure<LoginResponse>(IdentityErrors.User.Deactivated);

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result.Failure<LoginResponse>(IdentityErrors.User.InvalidCredentials);

        // Single DB call: role names + permissions combined (was 2 sequential queries)
        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var (roleNames, permissions) = await roleRepository.GetRolesAndPermissionsAsync(roleIds, cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, roleNames, permissions);

        var refreshToken = RefreshTokenEntity.Create(
            user.Id,
            tokenService.GenerateRefreshToken(),
            DateTime.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpirationDays));

        refreshTokenRepository.Add(refreshToken);

        return new LoginResponse(user.Id, user.DisplayName, accessToken, refreshToken.Token, user.IsOnboardingCompleted);
    }
}
