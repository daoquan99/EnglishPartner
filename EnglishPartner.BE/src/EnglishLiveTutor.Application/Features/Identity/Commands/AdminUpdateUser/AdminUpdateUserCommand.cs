using EnglishLiveTutor.Application.Common.Cqrs;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.Identity.Contracts;
using EnglishLiveTutor.Application.Features.Identity.Mapping;
using EnglishLiveTutor.Application.Features.Identity.Ports;
using EnglishLiveTutor.Domain.Identity.Roles;
using EnglishLiveTutor.Domain.Identity.Users;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.AdminUpdateUser;

public sealed record AdminUpdateUserCommand(Guid UserId, AdminUpdateUserRequest Request) : ICommand<UserProfileDto?>;

public sealed class AdminUpdateUserCommandHandler : ICommandHandler<AdminUpdateUserCommand, UserProfileDto?>
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IUnitOfWork _uow;
    private readonly IAppReadDbContext _readDb;
    private readonly IPasswordHasher _hasher;

    public AdminUpdateUserCommandHandler(
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

    public async Task<UserProfileDto?> HandleAsync(AdminUpdateUserCommand command, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdWithRolesAndTokensAsync(command.UserId, ct);
        if (user is null)
            return null;

        var req = command.Request;
        if (!string.IsNullOrWhiteSpace(req.Email))
        {
            user.UpdateEmail(req.Email);
        }

        user.UpdateProfile(
            req.FullName,
            user.AvatarUrl,
            req.PreferredCefrLevel,
            req.PreferredVoiceName,
            user.TimeZoneId);

        if (req.IsActive) user.Activate();
        else user.Deactivate();

        if (!string.IsNullOrWhiteSpace(req.NewPassword))
        {
            user.ChangePassword(_hasher.HashPassword(req.NewPassword));
        }

        if (req.RoleCodes is not null)
        {
            var normalizedCodes = req.RoleCodes
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim().ToUpperInvariant())
                .Distinct()
                .ToList();

            var roles = await _roleRepo.GetActiveByCodesAsync(normalizedCodes, ct);
            user.SetRoles(roles.Select(r => r.Id));
        }

        await _uow.SaveChangesAsync(ct);

        var (roleCodes, permissionCodes) = await IdentityProjectionHelper.ResolveUserRolesAndPermissionsAsync(_readDb, user.Id, ct);
        return IdentityProjectionHelper.MapToProfileDto(user, roleCodes, permissionCodes);
    }
}
