using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Identity.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.AssignUserRoles;

public sealed class AssignUserRolesCommandHandler : ICommandHandler<AssignUserRolesCommand, UserProfileDto>
{
    private readonly IUserRepository _userRepo;
    private readonly IAppDbContextFactory _dbFactory;

    public AssignUserRolesCommandHandler(
        IUserRepository userRepo,
        IAppDbContextFactory dbFactory)
    {
        _userRepo = userRepo;
        _dbFactory = dbFactory;
    }

    public async Task<UserProfileDto> HandleAsync(AssignUserRolesCommand command, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdWithRolesAndTokensAsync(command.UserId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy người dùng.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var normalizedCodes = (command.Request.RoleCodes ?? new List<string>())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        var roles = await db.Roles
            .AsNoTracking()
            .Where(r => normalizedCodes.Contains(r.Code) && r.IsActive)
            .ToListAsync(ct);

        user.SetRoles(roles.Select(r => r.Id));
        await _userRepo.SaveChangesAsync(ct);

        var (roleCodes, permissionCodes) = await IdentityProjectionHelper.ResolveUserRolesAndPermissionsAsync(db, user.Id, ct);
        return IdentityProjectionHelper.MapToProfileDto(user, roleCodes, permissionCodes);
    }
}
