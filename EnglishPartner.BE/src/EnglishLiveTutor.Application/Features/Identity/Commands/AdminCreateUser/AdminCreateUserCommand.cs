using EnglishLiveTutor.Application.Common.Cqrs;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.Identity.Contracts;
using EnglishLiveTutor.Application.Features.Identity.Mapping;
using EnglishLiveTutor.Application.Features.Identity.Ports;
using EnglishLiveTutor.Domain.Identity.Roles;
using EnglishLiveTutor.Domain.Identity.Users;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.AdminCreateUser;

public sealed record AdminCreateUserCommand(AdminCreateUserRequest Request) : ICommand<UserProfileDto>;

public sealed class AdminCreateUserCommandHandler : ICommandHandler<AdminCreateUserCommand, UserProfileDto>
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IUnitOfWork _uow;
    private readonly IAppReadDbContext _readDb;
    private readonly IPasswordHasher _hasher;

    public AdminCreateUserCommandHandler(
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        IUnitOfWork uow,
        IAppReadDbContext readDb,
        IPasswordHasher hasher)
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _uow = uow;
        _readDb = readDb;
        _hasher = hasher;
    }

    public async Task<UserProfileDto> HandleAsync(AdminCreateUserCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Email))
            throw new InvalidOperationException("Username và Email là bắt buộc.");

        string cleanUsername = req.Username.Trim();
        if (await _userRepo.ExistsByEmailOrUsernameAsync(req.Email, cleanUsername, ct))
            throw new InvalidOperationException("Username hoặc Email đã tồn tại.");

        var user = UserEntity.Create(
            cleanUsername,
            req.Email.Trim(),
            string.IsNullOrWhiteSpace(req.FullName) ? cleanUsername : req.FullName.Trim(),
            _hasher.HashPassword(string.IsNullOrWhiteSpace(req.Password) ? "Learner@123" : req.Password),
            req.PreferredCefrLevel,
            req.PreferredVoiceName);

        if (!req.IsActive)
        {
            user.Deactivate();
        }

        var roleCodes = req.RoleCodes is { Count: > 0 }
            ? req.RoleCodes.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim().ToUpperInvariant()).Distinct().ToList()
            : new List<string> { "LEARNER" };

        var roles = await _roleRepo.GetActiveByCodesAsync(roleCodes, ct);
        user.SetRoles(roles.Select(r => r.Id));

        await _userRepo.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        var (assignedRoleCodes, permissionCodes) = await IdentityProjectionHelper.ResolveUserRolesAndPermissionsAsync(_readDb, user.Id, ct);
        return IdentityProjectionHelper.MapToProfileDto(user, assignedRoleCodes, permissionCodes);
    }
}
