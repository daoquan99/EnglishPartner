using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Domain.Identity.Entities;
using EnglishLiveTutor.Domain.Identity.Repositories;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.Login;

public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepo;
    private readonly IAppDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtService;

    public LoginCommandHandler(
        IUserRepository userRepo,
        IAppDbContext db,
        IUnitOfWork uow,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtService)
    {
        _userRepo = userRepo;
        _db = db;
        _uow = uow;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<AuthResponseDto> HandleAsync(LoginCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.UsernameOrEmail) || string.IsNullOrWhiteSpace(req.Password))
            throw new InvalidOperationException("Vui lòng nhập tài khoản và mật khẩu.");

        var user = await _userRepo.GetByEmailOrUsernameWithDetailsAsync(req.UsernameOrEmail, ct);
        if (user is null || !user.IsActive)
            throw new InvalidOperationException("Tài khoản không tồn tại hoặc đã bị vô hiệu hóa.");

        if (!_passwordHasher.VerifyPassword(req.Password, user.PasswordHash))
            throw new InvalidOperationException("Thông tin đăng nhập không chính xác.");

        var now = DateTimeOffset.UtcNow;
        user.RecordLogin(now);

        string rawRefreshToken = _jwtService.GenerateRefreshTokenString();
        string tokenHash = RefreshTokenEntity.HashToken(rawRefreshToken);
        var refreshExpires = now.AddDays(30);

        user.IssueRefreshToken(tokenHash, refreshExpires, command.IpAddress, req.DeviceInfo);

        await _userRepo.PurgeExpiredOrRevokedRefreshTokensAsync(user.Id, now, ct);
        await _uow.CommitChangesAsync(ct);

        var (roleCodes, permissionCodes) = await IdentityProjectionHelper.ResolveUserRolesAndPermissionsAsync(_db, user.Id, ct);
        var accessToken = _jwtService.GenerateAccessToken(user, roleCodes, permissionCodes);

        return new AuthResponseDto(
            AccessToken: accessToken.AccessToken,
            AccessTokenExpiresAtUtc: accessToken.ExpiresAtUtc,
            RefreshToken: rawRefreshToken,
            RefreshTokenExpiresAtUtc: refreshExpires,
            User: IdentityProjectionHelper.MapToProfileDto(user, roleCodes, permissionCodes)
        );
    }
}
