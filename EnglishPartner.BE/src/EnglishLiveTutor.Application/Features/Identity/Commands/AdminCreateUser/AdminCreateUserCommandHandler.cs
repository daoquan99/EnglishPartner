using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.AdminCreateUser;

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
