using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Queries.ListUsers;

public sealed record ListUsersQuery() : IQuery<List<UserProfileDto>>;

public sealed class ListUsersQueryHandler : IQueryHandler<ListUsersQuery, List<UserProfileDto>>
{
    private readonly IAppDbContextFactory _dbFactory;

    public ListUsersQueryHandler(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<UserProfileDto>> HandleAsync(ListUsersQuery query, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var users = await db.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAtUtc)
            .ToListAsync(ct);

        var result = new List<UserProfileDto>(users.Count);
        foreach (var u in users)
        {
            var (roleCodes, permissionCodes) = await IdentityProjectionHelper.ResolveUserRolesAndPermissionsAsync(db, u.Id, ct);
            result.Add(IdentityProjectionHelper.MapToProfileDto(u, roleCodes, permissionCodes));
        }

        return result;
    }
}
