using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Domain.Identity.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.CreateRole;

public sealed record CreateRoleCommand(CreateRoleRequest Request) : ICommand<RoleDto>;

public sealed class CreateRoleCommandHandler : ICommandHandler<CreateRoleCommand, RoleDto>
{
    private readonly IRoleRepository _roleRepo;
    private readonly IAppDbContextFactory _dbFactory;

    public CreateRoleCommandHandler(IRoleRepository roleRepo, IAppDbContextFactory dbFactory)
    {
        _roleRepo = roleRepo;
        _dbFactory = dbFactory;
    }

    public async Task<RoleDto> HandleAsync(CreateRoleCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var existing = await _roleRepo.GetByCodeWithPermissionsAsync(req.Code, ct);
        if (existing is not null)
            throw new InvalidOperationException($"Vai trò '{req.Code}' đã tồn tại.");

        var role = new RoleEntity(req.Code, req.Name, req.Description, isSystemRole: false);

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

        foreach (var p in perms)
        {
            role.GrantPermission(p.Id);
        }

        await _roleRepo.AddAsync(role, ct);
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
