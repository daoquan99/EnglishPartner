using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.AdminManageIdentity;

// 1. Admin Create User
public sealed record AdminCreateUserCommand(AdminCreateUserRequest Request) : ICommand<UserProfileDto>;

public sealed class AdminCreateUserCommandHandler : ICommandHandler<AdminCreateUserCommand, UserProfileDto>
{
    private readonly IAppDbContextFactory _dbFactory;
    private readonly IPasswordHasher _hasher;

    public AdminCreateUserCommandHandler(IAppDbContextFactory dbFactory, IPasswordHasher hasher)
    {
        _dbFactory = dbFactory;
        _hasher = hasher;
    }

    public async Task<UserProfileDto> HandleAsync(AdminCreateUserCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Email))
            throw new InvalidOperationException("Username và Email là bắt buộc.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        string cleanUsername = req.Username.Trim();
        string normEmail = UserEntity.NormalizeEmail(req.Email);

        if (await db.Users.AnyAsync(u => u.Username == cleanUsername || u.NormalizedEmail == normEmail, ct))
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

        var roles = await db.Roles
            .AsNoTracking()
            .Where(r => roleCodes.Contains(r.Code) && r.IsActive)
            .ToListAsync(ct);

        user.SetRoles(roles.Select(r => r.Id));

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var (assignedRoleCodes, permissionCodes) = await IdentityProjectionHelper.ResolveUserRolesAndPermissionsAsync(db, user.Id, ct);
        return IdentityProjectionHelper.MapToProfileDto(user, assignedRoleCodes, permissionCodes);
    }
}

// 2. Admin Update User
public sealed record AdminUpdateUserCommand(Guid UserId, AdminUpdateUserRequest Request) : ICommand<UserProfileDto?>;

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

// 3. Admin Soft-Delete User
public sealed record DeleteUserCommand(Guid UserId) : ICommand<bool>;

public sealed class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand, bool>
{
    private readonly IAppDbContextFactory _dbFactory;
    private readonly ICurrentUser _currentUser;

    public DeleteUserCommandHandler(IAppDbContextFactory dbFactory, ICurrentUser currentUser)
    {
        _dbFactory = dbFactory;
        _currentUser = currentUser;
    }

    public async Task<bool> HandleAsync(DeleteUserCommand command, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user is null)
            return false;

        user.SoftDelete(DateTimeOffset.UtcNow, _currentUser.UserIdOrSystem);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

// 4. Admin Soft-Delete Role (blocks deleting System Roles)
public sealed record DeleteRoleCommand(Guid RoleId) : ICommand<bool>;

public sealed class DeleteRoleCommandHandler : ICommandHandler<DeleteRoleCommand, bool>
{
    private readonly IAppDbContextFactory _dbFactory;
    private readonly ICurrentUser _currentUser;

    public DeleteRoleCommandHandler(IAppDbContextFactory dbFactory, ICurrentUser currentUser)
    {
        _dbFactory = dbFactory;
        _currentUser = currentUser;
    }

    public async Task<bool> HandleAsync(DeleteRoleCommand command, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == command.RoleId, ct);
        if (role is null)
            return false;

        if (role.IsSystemRole)
            throw new InvalidOperationException($"Không được phép xóa vai trò hệ thống '{role.Code}'.");

        role.SoftDelete(DateTimeOffset.UtcNow, _currentUser.UserIdOrSystem);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

// 5. Admin Update Permission
public sealed record UpdatePermissionCommand(Guid PermissionId, AdminUpdatePermissionRequest Request) : ICommand<PermissionDto?>;

public sealed class UpdatePermissionCommandHandler : ICommandHandler<UpdatePermissionCommand, PermissionDto?>
{
    private readonly IAppDbContextFactory _dbFactory;

    public UpdatePermissionCommandHandler(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<PermissionDto?> HandleAsync(UpdatePermissionCommand command, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var perm = await db.Permissions.FirstOrDefaultAsync(p => p.Id == command.PermissionId, ct);
        if (perm is null)
            return null;

        perm.UpdateDetails(command.Request.Name, command.Request.Module, command.Request.Description);
        await db.SaveChangesAsync(ct);
        return new PermissionDto(perm.Id, perm.Code, perm.Name, perm.Module, perm.Description);
    }
}

// 6. Admin Soft-Delete Permission
public sealed record DeletePermissionCommand(Guid PermissionId) : ICommand<bool>;

public sealed class DeletePermissionCommandHandler : ICommandHandler<DeletePermissionCommand, bool>
{
    private readonly IAppDbContextFactory _dbFactory;
    private readonly ICurrentUser _currentUser;

    public DeletePermissionCommandHandler(IAppDbContextFactory dbFactory, ICurrentUser currentUser)
    {
        _dbFactory = dbFactory;
        _currentUser = currentUser;
    }

    public async Task<bool> HandleAsync(DeletePermissionCommand command, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var perm = await db.Permissions.FirstOrDefaultAsync(p => p.Id == command.PermissionId, ct);
        if (perm is null)
            return false;

        perm.SoftDelete(DateTimeOffset.UtcNow, _currentUser.UserIdOrSystem);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
