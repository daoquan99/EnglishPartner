using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Modules.Identity;
using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Domain.Identity.Entities;
using EnglishLiveTutor.Domain.Identity.Repositories;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IIdentityDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtService;

    public RegisterUserCommandHandler(
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        IIdentityDbContext db,
        IUnitOfWork uow,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtService)
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _db = db;
        _uow = uow;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<AuthResponseDto> HandleAsync(RegisterUserCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            throw new InvalidOperationException("Username, Email và Password là bắt buộc.");

        if (req.Password.Length < 6)
            throw new InvalidOperationException("Mật khẩu phải có ít nhất 6 ký tự.");

        bool exists = await _userRepo.ExistsByEmailOrUsernameAsync(req.Email, req.Username, ct);
        if (exists)
            throw new InvalidOperationException("Email hoặc Username đã được sử dụng.");

        string passwordHash = _passwordHasher.HashPassword(req.Password);
        var user = UserEntity.Create(
            username: req.Username,
            email: req.Email,
            fullName: string.IsNullOrWhiteSpace(req.FullName) ? req.Username : req.FullName,
            passwordHash: passwordHash,
            preferredCefrLevel: req.PreferredCefrLevel,
            preferredVoiceName: req.PreferredVoiceName,
            timeZoneId: req.TimeZoneId);

        var learnerRole = await _roleRepo.GetByCodeWithPermissionsAsync("LEARNER", ct)
            ?? throw new InvalidOperationException("Hệ thống chưa thiết lập vai trò LEARNER. Vui lòng liên hệ quản trị viên.");

        user.AssignRole(learnerRole.Id);

        var now = DateTimeOffset.UtcNow;
        user.RecordLogin(now);

        string rawRefreshToken = _jwtService.GenerateRefreshTokenString();
        string tokenHash = RefreshTokenEntity.HashToken(rawRefreshToken);
        var refreshExpires = now.AddDays(30);

        user.IssueRefreshToken(tokenHash, refreshExpires, command.IpAddress, "Registration");

        await _userRepo.AddAsync(user, ct);
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
