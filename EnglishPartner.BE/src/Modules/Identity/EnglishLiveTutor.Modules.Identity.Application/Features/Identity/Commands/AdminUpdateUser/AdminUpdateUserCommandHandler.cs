using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Modules.Identity;
using EnglishLiveTutor.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.AdminUpdateUser;

public sealed class AdminUpdateUserCommandHandler : ICommandHandler<AdminUpdateUserCommand, UserProfileDto?>
{
    private readonly IIdentityDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;

    public AdminUpdateUserCommandHandler(IIdentityDbContext db, IUnitOfWork uow, IPasswordHasher hasher)
    {
        _db = db;
        _uow = uow;
        _hasher = hasher;
    }

    public async Task<UserProfileDto?> HandleAsync(AdminUpdateUserCommand command, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct);

        if (user is null)
            return null;

        var req = command.Request;
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

            var roleIds = await _db.Roles
                .AsNoTracking()
                .Where(r => normalizedCodes.Contains(r.Code) && r.IsActive)
                .Select(r => r.Id)
                .ToListAsync(ct);

            user.SetRoles(roleIds);
        }

        await _uow.CommitChangesAsync(ct);

        var (roleCodes, permissionCodes) = await IdentityProjectionHelper.ResolveUserRolesAndPermissionsAsync(_db, user.Id, ct);
        return IdentityProjectionHelper.MapToProfileDto(user, roleCodes, permissionCodes);
    }
}
