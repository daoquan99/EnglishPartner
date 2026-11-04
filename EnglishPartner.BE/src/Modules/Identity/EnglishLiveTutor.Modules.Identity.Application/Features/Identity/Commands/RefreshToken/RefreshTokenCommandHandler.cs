using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Modules.Identity;
using EnglishLiveTutor.Domain.Identity.Entities;
using EnglishLiveTutor.Domain.Identity.Repositories;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepo;
    private readonly IIdentityDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly IJwtTokenService _jwtService;
    private readonly RefreshTokenOptions _options;
    private readonly TimeProvider _timeProvider;

    public RefreshTokenCommandHandler(
        IUserRepository userRepo,
        IIdentityDbContext db,
        IUnitOfWork uow,
        IJwtTokenService jwtService,
        RefreshTokenOptions? options = null,
        TimeProvider? timeProvider = null)
    {
        _userRepo = userRepo;
        _db = db;
        _uow = uow;
        _jwtService = jwtService;
        _options = options ?? new RefreshTokenOptions();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<AuthResponseDto> HandleAsync(RefreshTokenCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.RefreshToken))
            throw new InvalidOperationException("RefreshToken không hợp lệ.");

        string rawToken = req.RefreshToken.Trim();
        string tokenHash = RefreshTokenEntity.HashToken(rawToken);

        var user = await _userRepo.GetByRefreshTokenAsync(tokenHash, ct);
        if (user is null || !user.IsActive)
            throw new InvalidOperationException("RefreshToken không hợp lệ.");

        var existingToken = user.RefreshTokens.FirstOrDefault(t => t.Token == tokenHash);
        if (existingToken is null)
            throw new InvalidOperationException("RefreshToken không hợp lệ.");

        var now = _timeProvider.GetUtcNow();
        int graceSeconds = _options.GraceSeconds;

        // Phát hiện tái sử dụng refresh token đã rotate (S8 & L1 khoảng ân hạn)
        if (existingToken.IsRevoked && !string.IsNullOrWhiteSpace(existingToken.ReplacedByToken))
        {
            var revokedAt = existingToken.RevokedAtUtc ?? now;
            var elapsed = now - revokedAt;

            // Trong khoảng ân hạn (nhiều tab gửi đồng thời): từ chối 401 nhưng KHÔNG revoke toàn bộ phiên
            if (elapsed >= TimeSpan.Zero && elapsed <= TimeSpan.FromSeconds(graceSeconds))
            {
                throw new InvalidOperationException("RefreshToken không hợp lệ.");
            }

            // Quá khoảng ân hạn: revoke toàn bộ phiên
            await _userRepo.RevokeAllActiveRefreshTokensAsync(user.Id, now, "Reuse detected", ct);
            await _uow.CommitChangesAsync(ct);
            throw new InvalidOperationException("RefreshToken không hợp lệ.");
        }

        if (!existingToken.IsActive(now))
        {
            throw new InvalidOperationException("RefreshToken không hợp lệ.");
        }

        string newRawToken = _jwtService.GenerateRefreshTokenString();
        string newTokenHash = RefreshTokenEntity.HashToken(newRawToken);
        var newRefreshExpires = now.AddDays(30);

        var newToken = new RefreshTokenEntity(
            userId: user.Id,
            token: newTokenHash,
            expiresAtUtc: newRefreshExpires,
            createdByIp: command.IpAddress,
            deviceInfo: req.DeviceInfo);

        // M1: Revoke token cũ có điều kiện (Id = @id AND RevokedAtUtc IS NULL).
        // Token mới chỉ được lưu sau khi revoke có điều kiện thành công trong cùng transaction.
        bool rotated = await _userRepo.RotateRefreshTokenConditionallyAsync(
            existingToken.Id,
            newToken,
            now,
            ct);

        if (!rotated)
        {
            // 0 dòng bị ảnh hưởng -> coi như token đã bị dùng (xử lý như reuse ở mục 3), không cấp token mới
            var currentTokenInDb = await _userRepo.GetRefreshTokenByIdAsync(existingToken.Id, ct);
            if (currentTokenInDb?.RevokedAtUtc != null && !string.IsNullOrWhiteSpace(currentTokenInDb.ReplacedByToken))
            {
                var elapsed = now - currentTokenInDb.RevokedAtUtc.Value;
                if (elapsed > TimeSpan.FromSeconds(graceSeconds))
                {
                    await _userRepo.RevokeAllActiveRefreshTokensAsync(user.Id, now, "Reuse detected", ct);
                    await _uow.CommitChangesAsync(ct);
                }
            }

            throw new InvalidOperationException("RefreshToken không hợp lệ.");
        }

        await _userRepo.PurgeExpiredOrRevokedRefreshTokensAsync(user.Id, now, ct);
        await _uow.CommitChangesAsync(ct);

        var (roleCodes, permissionCodes) = await IdentityProjectionHelper.ResolveUserRolesAndPermissionsAsync(_db, user.Id, ct);
        var accessToken = _jwtService.GenerateAccessToken(user, roleCodes, permissionCodes);

        return new AuthResponseDto(
            AccessToken: accessToken.AccessToken,
            AccessTokenExpiresAtUtc: accessToken.ExpiresAtUtc,
            RefreshToken: newRawToken,
            RefreshTokenExpiresAtUtc: newRefreshExpires,
            User: IdentityProjectionHelper.MapToProfileDto(user, roleCodes, permissionCodes)
        );
    }
}
