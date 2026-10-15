using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Queries.ListRoles;

public sealed record ListRolesQuery(
    string? Search = null,
    int? Page = null,
    int? PageSize = null
) : IQuery<PagedResultDto<RoleDto>>;

public sealed class ListRolesQueryHandler : IQueryHandler<ListRolesQuery, PagedResultDto<RoleDto>>
{
    private readonly IAppDbContextFactory _dbFactory;

    public ListRolesQueryHandler(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<PagedResultDto<RoleDto>> HandleAsync(ListRolesQuery query, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var q = db.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim().ToLowerInvariant();
            q = q.Where(r =>
                r.Code.ToLower().Contains(term) ||
                r.Name.ToLower().Contains(term) ||
                r.Description.ToLower().Contains(term));
        }

        var ordered = q.OrderBy(r => r.Code);

        return await PagedResultDto<RoleEntity>.CreateProjectedAsync(
            ordered,
            async (pageRoles, token) =>
            {
                var permsById = await db.Permissions
                    .AsNoTracking()
                    .ToDictionaryAsync(p => p.Id, p => p.Code, token);

                return pageRoles.Select(r => new RoleDto(
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
            },
            query.Page,
            query.PageSize,
            ct);
    }
}
