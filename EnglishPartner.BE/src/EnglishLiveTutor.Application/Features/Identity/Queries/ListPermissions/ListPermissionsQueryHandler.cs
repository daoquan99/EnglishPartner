using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Queries.ListPermissions;

public sealed class ListPermissionsQueryHandler : IQueryHandler<ListPermissionsQuery, PagedResultDto<PermissionDto>>
{
    private readonly IAppDbContextFactory _dbFactory;

    public ListPermissionsQueryHandler(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<PagedResultDto<PermissionDto>> HandleAsync(ListPermissionsQuery query, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var q = db.Permissions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim().ToLowerInvariant();
            q = q.Where(p =>
                p.Code.ToLower().Contains(term) ||
                p.Name.ToLower().Contains(term) ||
                p.Module.ToLower().Contains(term) ||
                p.Description.ToLower().Contains(term));
        }

        var projected = q
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Code)
            .Select(p => new PermissionDto(p.Id, p.Code, p.Name, p.Module, p.Description));

        return await PagedResultDto<PermissionDto>.CreateAsync(projected, query.Page, query.PageSize, ct);
    }
}
