using EnglishLiveTutor.Application.Features.Identity.Contracts;
using EnglishLiveTutor.Application.Modules.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Services;

public sealed class UserDirectory(IIdentityDbContext db) : IUserDirectory
{
    public Task<UserLearningProfile?> GetLearningProfileAsync(
        string userId,
        CancellationToken ct = default)
    {
        return db.Users
            .AsNoTracking()
            .Where(u => u.Id.ToString() == userId || u.Username == userId)
            .Select(u => new UserLearningProfile(u.TimeZoneId, u.PreferredCefrLevel))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<UserNameInfo>> ListUsersByIdOrUsernameAsync(
        IReadOnlyCollection<string> idsOrUsernames,
        CancellationToken ct = default)
    {
        return await db.Users
            .AsNoTracking()
            .Where(u => idsOrUsernames.Contains(u.Id.ToString()) || idsOrUsernames.Contains(u.Username))
            .Select(u => new UserNameInfo(u.Id, u.Username, u.FullName))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<UserNameInfo>> ListUsersByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default)
    {
        return await db.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new UserNameInfo(u.Id, u.Username, u.FullName))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<UserNameInfo>> ListUsersByUsernamesAsync(
        IReadOnlyCollection<string> usernames,
        CancellationToken ct = default)
    {
        return await db.Users
            .AsNoTracking()
            .Where(u => usernames.Contains(u.Username))
            .Select(u => new UserNameInfo(u.Id, u.Username, u.FullName))
            .ToListAsync(ct);
    }
}
