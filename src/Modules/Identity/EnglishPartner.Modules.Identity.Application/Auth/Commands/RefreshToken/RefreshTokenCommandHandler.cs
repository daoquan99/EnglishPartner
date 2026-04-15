using EnglishPartner.Modules.Identity.Application.Abstractions;
using EnglishPartner.Modules.Identity.Domain;
using EnglishPartner.Modules.Identity.Domain.Roles.Repositories;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;
using EnglishPartner.SharedKernel.Settings;
using Microsoft.Extensions.Options;
using RefreshTokenEntity = EnglishPartner.Modules.Identity.Domain.Users.Entities.RefreshToken;

namespace EnglishPartner.Modules.Identity.Application.Auth.Commands.RefreshToken;

internal sealed class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    ITokenService tokenService,
    IOptions<JwtSettings> jwtSettings) : ICommandHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var existingToken = await refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (existingToken is null || !existingToken.IsValid)
            return Result.Failure<RefreshTokenResponse>(IdentityErrors.Token.InvalidRefreshToken);

        existingToken.Revoke();

        var user = await userRepository.GetByIdAsync(existingToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
            return Result.Failure<RefreshTokenResponse>(IdentityErrors.User.Deactivated);

        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var roleNames   = await roleRepository.GetRoleNamesByIdsAsync(roleIds, cancellationToken);
        var permissions = await roleRepository.GetPermissionCodesByRoleIdsAsync(roleIds, cancellationToken);
        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, roleNames, permissions);

        var newRefreshToken = RefreshTokenEntity.Create(
            user.Id,
            tokenService.GenerateRefreshToken(),
            DateTime.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpirationDays));

        refreshTokenRepository.Add(newRefreshToken);

        return new RefreshTokenResponse(accessToken, newRefreshToken.Token);
    }
}
