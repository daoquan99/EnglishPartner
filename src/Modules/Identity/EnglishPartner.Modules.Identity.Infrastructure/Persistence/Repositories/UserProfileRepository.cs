using EnglishPartner.Modules.Identity.Domain.Users.Entities;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Identity.Infrastructure.Persistence.Repositories;

internal sealed class UserProfileRepository(IdentityDbContext context) : IUserProfileRepository
{
    public async Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public void Add(UserProfile profile) => context.UserProfiles.Add(profile);

    public void Update(UserProfile profile) => context.UserProfiles.Update(profile);
}
