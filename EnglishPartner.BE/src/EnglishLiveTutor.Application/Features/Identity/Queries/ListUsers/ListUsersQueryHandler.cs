using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Queries.ListUsers;

public sealed class ListUsersQueryHandler : IQueryHandler<ListUsersQuery, PagedResultDto<UserProfileDto>>
{
    private readonly IAppDbContextFactory _dbFactory;

    public ListUsersQueryHandler(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<PagedResultDto<UserProfileDto>> HandleAsync(ListUsersQuery query, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var q = db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim().ToLowerInvariant();
            string normEmailTerm = query.Search.Trim().ToUpperInvariant();
            q = q.Where(u =>
                u.Username.ToLower().Contains(term) ||
                u.NormalizedEmail.Contains(normEmailTerm) ||
                u.FullName.ToLower().Contains(term) ||
                u.UserRoles.Any(ur => db.Roles.Any(r => r.Id == ur.RoleId && r.Code.ToLower().Contains(term))));
        }

        var ordered = q.OrderByDescending(u => u.CreatedAtUtc);

        return await PagedResultDto<UserEntity>.CreateProjectedAsync(
            ordered,
            async (pageUsers, token) =>
            {
                var userIds = pageUsers.Select(u => u.Id).ToList();
                var batchMap = await IdentityProjectionHelper.ResolveUsersRolesAndPermissionsBatchAsync(db, userIds, token);
                var list = new List<UserProfileDto>(pageUsers.Count);
                foreach (var u in pageUsers)
                {
                    var (roleCodes, permissionCodes) = batchMap.TryGetValue(u.Id, out var tuple)
                        ? tuple
                        : (new List<string>(), new List<string>());
                    list.Add(IdentityProjectionHelper.MapToProfileDto(u, roleCodes, permissionCodes));
                }
                return list;
            },
            query.Page,
            query.PageSize,
            ct);
    }
}
