using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Domain.Identity.Repositories;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IAppDbContextFactory _dbFactory;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtService;

    public RegisterUserCommandHandler(
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        IAppDbContextFactory dbFactory,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtService)
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _dbFactory = dbFactory;
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

        var learnerRole = await _roleRepo.GetByCodeWithPermissionsAsync("LEARNER", ct);
        if (learnerRole is not null)
        {
            user.AssignRole(learnerRole.Id);
        }

        var now = DateTimeOffset.UtcNow;
        user.RecordLogin(now);

        string refreshTokenStr = _jwtService.GenerateRefreshTokenString();
        var refreshExpires = now.AddDays(30);
        user.IssueRefreshToken(refreshTokenStr, refreshExpires, command.IpAddress, "Registration");

        await _userRepo.AddAsync(user, ct);
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
