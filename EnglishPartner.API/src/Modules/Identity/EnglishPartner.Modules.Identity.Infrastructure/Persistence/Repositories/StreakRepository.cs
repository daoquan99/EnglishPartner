using EnglishPartner.Modules.Identity.Domain.Users.Entities;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Identity.Infrastructure.Persistence.Repositories;

internal sealed class StreakRepository(IdentityDbContext context) : IStreakRepository
{
    public async Task<Streak?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.Streaks.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

    public void Add(Streak streak) => context.Streaks.Add(streak);

    public void Update(Streak streak) => context.Streaks.Update(streak);
}
