using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Identity.Repositories;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.Login;

public sealed record LoginCommand(
    LoginRequest Request,
    string? IpAddress = null
) : ICommand<AuthResponseDto>;

public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepo;
    private readonly IAppDbContextFactory _dbFactory;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtService;

    public LoginCommandHandler(
        IUserRepository userRepo,
        IAppDbContextFactory dbFactory,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtService)
    {
        _userRepo = userRepo;
        _dbFactory = dbFactory;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<AuthResponseDto> HandleAsync(LoginCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.EmailOrUsername) || string.IsNullOrWhiteSpace(req.Password))
            throw new InvalidOperationException("Vui lòng nhập tài khoản và mật khẩu.");

        var user = await _userRepo.GetByEmailOrUsernameWithDetailsAsync(req.EmailOrUsername, ct);
        if (user is null || !user.IsActive)
            throw new InvalidOperationException("Tài khoản không tồn tại hoặc đã bị vô hiệu hóa.");

        if (!_passwordHasher.VerifyPassword(req.Password, user.PasswordHash))
            throw new InvalidOperationException("Thông tin đăng nhập không chính xác.");

        var now = DateTimeOffset.UtcNow;
        user.RecordLogin(now);

        string refreshTokenStr = _jwtService.GenerateRefreshTokenString();
        var refreshExpires = now.AddDays(30);
        user.IssueRefreshToken(refreshTokenStr, refreshExpires, command.IpAddress, req.DeviceInfo);

        await _userRepo.SaveChangesAsync(ct);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var (roleCodes, permissionCodes) = await IdentityProjectionHelper.ResolveUserRolesAndPermissionsAsync(db, user.Id, ct);
        var accessToken = _jwtService.GenerateAccessToken(user, roleCodes, permissionCodes);

        return new AuthResponseDto(
            AccessToken: accessToken.AccessToken,
            AccessTokenExpiresAtUtc: accessToken.ExpiresAtUtc,
            RefreshToken: refreshTokenStr,
            RefreshTokenExpiresAtUtc: refreshExpires,
            User: IdentityProjectionHelper.MapToProfileDto(user, roleCodes, permissionCodes)
        );
    }
}
