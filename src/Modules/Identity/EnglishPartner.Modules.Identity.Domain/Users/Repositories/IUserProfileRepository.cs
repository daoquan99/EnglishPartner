using EnglishPartner.Modules.Identity.Domain.Users.Entities;

namespace EnglishPartner.Modules.Identity.Domain.Users.Repositories;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    void Add(UserProfile profile);
    void Update(UserProfile profile);
}
