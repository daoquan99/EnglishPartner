using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Identity.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.UpdateRolePermissions;

public sealed record UpdateRolePermissionsCommand(
    Guid RoleId,
    UpdateRolePermissionsRequest Request
) : ICommand<RoleDto>;

public sealed class UpdateRolePermissionsCommandHandler : ICommandHandler<UpdateRolePermissionsCommand, RoleDto>
{
    private readonly IRoleRepository _roleRepo;
    private readonly IAppDbContextFactory _dbFactory;

    public UpdateRolePermissionsCommandHandler(IRoleRepository roleRepo, IAppDbContextFactory dbFactory)
    {
        _roleRepo = roleRepo;
        _dbFactory = dbFactory;
    }

    public async Task<RoleDto> HandleAsync(UpdateRolePermissionsCommand command, CancellationToken ct = default)
    {
        var role = await _roleRepo.GetByIdWithPermissionsAsync(command.RoleId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy vai trò.");

        var req = command.Request;
        role.UpdateDetails(req.Name ?? role.Name, req.Description ?? role.Description);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var permCodes = (req.PermissionCodes ?? new List<string>())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        var perms = await db.Permissions
            .AsNoTracking()
            .Where(p => permCodes.Contains(p.Code))
            .ToListAsync(ct);

        role.SetPermissions(perms.Select(p => p.Id));
        await _roleRepo.SaveChangesAsync(ct);

        return new RoleDto(
            role.Id,
            role.Code,
            role.Name,
            role.Description,
            role.IsSystemRole,
            role.IsActive,
            perms.Select(p => p.Code).OrderBy(c => c).ToList());
    }
}
