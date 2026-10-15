using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Identity.Repositories;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.RefreshToken;

public sealed record RefreshTokenCommand(
    RefreshTokenRequest Request,
    string? IpAddress = null
) : ICommand<AuthResponseDto>;

public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepo;
    private readonly IAppDbContextFactory _dbFactory;
    private readonly IJwtTokenService _jwtService;

    public RefreshTokenCommandHandler(
        IUserRepository userRepo,
        IAppDbContextFactory dbFactory,
        IJwtTokenService jwtService)
    {
        _userRepo = userRepo;
        _dbFactory = dbFactory;
        _jwtService = jwtService;
    }

    public async Task<AuthResponseDto> HandleAsync(RefreshTokenCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.RefreshToken))
            throw new InvalidOperationException("RefreshToken không được để trống.");

        var user = await _userRepo.GetByRefreshTokenAsync(req.RefreshToken.Trim(), ct);
        if (user is null || !user.IsActive)
            throw new InvalidOperationException("RefreshToken không hợp lệ.");

        var now = DateTimeOffset.UtcNow;
        string newRefreshTokenStr = _jwtService.GenerateRefreshTokenString();
        var newRefreshExpires = now.AddDays(30);

        user.RotateRefreshToken(
            currentToken: req.RefreshToken.Trim(),
            newToken: newRefreshTokenStr,
            newExpiresAtUtc: newRefreshExpires,
            nowUtc: now,
            ipAddress: command.IpAddress,
            deviceInfo: req.DeviceInfo);

        await _userRepo.SaveChangesAsync(ct);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var (roleCodes, permissionCodes) = await IdentityProjectionHelper.ResolveUserRolesAndPermissionsAsync(db, user.Id, ct);
        var accessToken = _jwtService.GenerateAccessToken(user, roleCodes, permissionCodes);

        return new AuthResponseDto(
            AccessToken: accessToken.AccessToken,
            AccessTokenExpiresAtUtc: accessToken.ExpiresAtUtc,
            RefreshToken: newRefreshTokenStr,
            RefreshTokenExpiresAtUtc: newRefreshExpires,
            User: IdentityProjectionHelper.MapToProfileDto(user, roleCodes, permissionCodes)
        );
    }
}
