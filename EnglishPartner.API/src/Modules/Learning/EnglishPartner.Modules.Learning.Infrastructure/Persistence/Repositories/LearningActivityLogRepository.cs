using EnglishPartner.Modules.Learning.Domain.StudySessions;
using EnglishPartner.Modules.Learning.Domain.StudySessions.Repositories;

namespace EnglishPartner.Modules.Learning.Infrastructure.Persistence.Repositories;

internal sealed class LearningActivityLogRepository(LearningDbContext dbContext) : ILearningActivityLogRepository
{
    public void Add(LearningActivityLog log) => dbContext.ActivityLogs.Add(log);
}
