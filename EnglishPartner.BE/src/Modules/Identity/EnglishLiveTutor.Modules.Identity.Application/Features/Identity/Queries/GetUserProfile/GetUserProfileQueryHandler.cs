using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Modules;
using EnglishLiveTutor.Application.Modules.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Queries.GetUserProfile;

public sealed class GetUserProfileQueryHandler : IQueryHandler<GetUserProfileQuery, UserProfileDto?>
{
    private readonly IModuleDbContextFactory<IIdentityDbContext> _dbFactory;

    public GetUserProfileQueryHandler(IModuleDbContextFactory<IIdentityDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<UserProfileDto?> HandleAsync(GetUserProfileQuery query, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == query.UserId, ct);

        if (user is null) return null;

        var (roleCodes, permissionCodes) = await IdentityProjectionHelper.ResolveUserRolesAndPermissionsAsync(db, user.Id, ct);
        return IdentityProjectionHelper.MapToProfileDto(user, roleCodes, permissionCodes);
    }
}
