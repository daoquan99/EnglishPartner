using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Queries.ListRoles;

public sealed record ListRolesQuery() : IQuery<List<RoleDto>>;

public sealed class ListRolesQueryHandler : IQueryHandler<ListRolesQuery, List<RoleDto>>
{
    private readonly IAppDbContextFactory _dbFactory;

    public ListRolesQueryHandler(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<RoleDto>> HandleAsync(ListRolesQuery query, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var roles = await db.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
            .OrderBy(r => r.Code)
            .ToListAsync(ct);

        var permsById = await db.Permissions
            .AsNoTracking()
            .ToDictionaryAsync(p => p.Id, p => p.Code, ct);

        return roles.Select(r => new RoleDto(
            Id: r.Id,
            Code: r.Code,
            Name: r.Name,
            Description: r.Description,
            IsSystemRole: r.IsSystemRole,
            IsActive: r.IsActive,
            Permissions: r.RolePermissions
                .Select(rp => permsById.TryGetValue(rp.PermissionId, out var code) ? code : null)
                .Where(c => c is not null)
                .Select(c => c!)
                .OrderBy(c => c)
                .ToList()
        )).ToList();
    }
}
