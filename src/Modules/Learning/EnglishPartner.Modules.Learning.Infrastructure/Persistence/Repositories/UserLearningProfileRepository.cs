using EnglishPartner.Modules.Learning.Domain.UserProfiles;
using EnglishPartner.Modules.Learning.Domain.UserProfiles.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Learning.Infrastructure.Persistence.Repositories;

internal sealed class UserLearningProfileRepository(LearningDbContext dbContext) : IUserLearningProfileRepository
{
    public async Task<UserLearningProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        => await dbContext.UserLearningProfiles.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public void Add(UserLearningProfile profile) => dbContext.UserLearningProfiles.Add(profile);
    public void Update(UserLearningProfile profile) => dbContext.UserLearningProfiles.Update(profile);
}
