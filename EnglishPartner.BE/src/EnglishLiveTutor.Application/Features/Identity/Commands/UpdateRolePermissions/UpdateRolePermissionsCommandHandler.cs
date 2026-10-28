using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Domain.Identity.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.UpdateRolePermissions;

public sealed class UpdateRolePermissionsCommandHandler : ICommandHandler<UpdateRolePermissionsCommand, RoleDto>
{
    private readonly IRoleRepository _roleRepo;
    private readonly IAppDbContext _db;
    private readonly IUnitOfWork _uow;

    public UpdateRolePermissionsCommandHandler(IRoleRepository roleRepo, IAppDbContext db, IUnitOfWork uow)
    {
        _roleRepo = roleRepo;
        _db = db;
        _uow = uow;
    }

    public async Task<RoleDto> HandleAsync(UpdateRolePermissionsCommand command, CancellationToken ct = default)
    {
        var role = await _roleRepo.GetByIdWithPermissionsAsync(command.RoleId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy vai trò.");

        var req = command.Request;
        role.UpdateDetails(req.Name ?? role.Name, req.Description ?? role.Description);

        var permCodes = (req.PermissionCodes ?? new List<string>())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        var perms = await _db.Permissions
            .AsNoTracking()
            .Where(p => permCodes.Contains(p.Code))
            .ToListAsync(ct);

        role.SetPermissions(perms.Select(p => p.Id));
        await _uow.CommitChangesAsync(ct);

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
