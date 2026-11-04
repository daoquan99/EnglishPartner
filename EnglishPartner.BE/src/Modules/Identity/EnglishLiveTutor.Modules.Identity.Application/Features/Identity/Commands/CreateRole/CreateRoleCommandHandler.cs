using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Modules.Identity;
using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Domain.Identity.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.CreateRole;

public sealed class CreateRoleCommandHandler : ICommandHandler<CreateRoleCommand, RoleDto>
{
    private readonly IRoleRepository _roleRepo;
    private readonly IIdentityDbContext _db;
    private readonly IUnitOfWork _uow;

    public CreateRoleCommandHandler(IRoleRepository roleRepo, IIdentityDbContext db, IUnitOfWork uow)
    {
        _roleRepo = roleRepo;
        _db = db;
        _uow = uow;
    }

    public async Task<RoleDto> HandleAsync(CreateRoleCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var existing = await _roleRepo.GetByCodeWithPermissionsAsync(req.Code, ct);
        if (existing is not null)
            throw new InvalidOperationException($"Vai trò '{req.Code}' đã tồn tại.");

        var role = new RoleEntity(req.Code, req.Name, req.Description, isSystemRole: false);

        var permCodes = (req.PermissionCodes ?? new List<string>())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        var perms = await _db.Permissions
            .AsNoTracking()
            .Where(p => permCodes.Contains(p.Code))
            .ToListAsync(ct);

        foreach (var p in perms)
        {
            role.GrantPermission(p.Id);
        }

        await _roleRepo.AddAsync(role, ct);
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
