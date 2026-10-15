using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.AdminUpdateUser;

public sealed class AdminUpdateUserCommandHandler : ICommandHandler<AdminUpdateUserCommand, UserProfileDto?>
{
    private readonly IAppDbContextFactory _dbFactory;
    private readonly IPasswordHasher _hasher;

    public AdminUpdateUserCommandHandler(IAppDbContextFactory dbFactory, IPasswordHasher hasher)
    {
        _dbFactory = dbFactory;
        _hasher = hasher;
    }

    public async Task<UserProfileDto?> HandleAsync(AdminUpdateUserCommand command, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var user = await db.Users
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

            var roleIds = await db.Roles
                .AsNoTracking()
                .Where(r => normalizedCodes.Contains(r.Code) && r.IsActive)
                .Select(r => r.Id)
                .ToListAsync(ct);

            user.SetRoles(roleIds);
        }

        await db.SaveChangesAsync(ct);

        var (roleCodes, permissionCodes) = await IdentityProjectionHelper.ResolveUserRolesAndPermissionsAsync(db, user.Id, ct);
        return IdentityProjectionHelper.MapToProfileDto(user, roleCodes, permissionCodes);
    }
}
