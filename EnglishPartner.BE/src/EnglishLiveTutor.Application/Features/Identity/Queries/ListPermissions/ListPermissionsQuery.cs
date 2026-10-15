using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Queries.ListPermissions;

public sealed record ListPermissionsQuery() : IQuery<List<PermissionDto>>;

public sealed class ListPermissionsQueryHandler : IQueryHandler<ListPermissionsQuery, List<PermissionDto>>
{
    private readonly IAppDbContextFactory _dbFactory;

    public ListPermissionsQueryHandler(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<PermissionDto>> HandleAsync(ListPermissionsQuery query, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Code)
            .Select(p => new PermissionDto(p.Id, p.Code, p.Name, p.Module, p.Description))
            .ToListAsync(ct);
    }
}
