namespace EnglishPartner.Modules.Learning.Domain.UserProfiles.Repositories;

public interface IUserLearningProfileRepository
{
    Task<UserLearningProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    void Add(UserLearningProfile profile);
    void Update(UserLearningProfile profile);
}
