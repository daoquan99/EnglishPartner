using EnglishPartner.Modules.Identity.Domain.Users.Entities;

namespace EnglishPartner.Modules.Identity.Domain.Users.Repositories;

public interface IStreakRepository
{
    Task<Streak?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    void Add(Streak streak);
    void Update(Streak streak);
}
