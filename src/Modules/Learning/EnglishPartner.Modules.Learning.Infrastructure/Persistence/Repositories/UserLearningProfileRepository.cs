using EnglishPartner.Modules.Learning.Domain.UserProfiles;
using EnglishPartner.Modules.Learning.Domain.UserProfiles.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Learning.Infrastructure.Persistence.Repositories;

internal sealed class UserLearningProfileRepository(LearningDbContext dbContext) : IUserLearningProfileRepository
{
    public async Task<UserLearningProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        => await dbContext.UserLearningProfiles.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<UserLearningProfile>> GetLeaderboardAsync(int topCount, CancellationToken cancellationToken)
    {
        return await dbContext.UserLearningProfiles
            .AsNoTracking()
            .OrderByDescending(x => x.TotalXp)
            .Take(topCount)
            .ToListAsync(cancellationToken);
    }

    public void Add(UserLearningProfile profile) => dbContext.UserLearningProfiles.Add(profile);
    public void Update(UserLearningProfile profile) => dbContext.UserLearningProfiles.Update(profile);

    public async Task<IReadOnlyList<ReminderTarget>> GetUsersForReminderAsync(int utcHour, CancellationToken cancellationToken)
    {
        // (local_hour - utc_offset + 24) % 24 == utcHour
        // Computed in-process after fetching candidates with non-null reminders (small set in practice).
        var candidates = await dbContext.UserLearningProfiles
            .AsNoTracking()
            .Where(x => x.Preferences.MorningReminderHour != null || x.Preferences.EveningReminderHour != null)
            .Select(x => new
            {
                x.UserId,
                x.Email,
                x.Preferences.DailyGoalMinutes,
                x.Preferences.MorningReminderHour,
                x.Preferences.EveningReminderHour,
                x.Preferences.UtcOffsetHours
            })
            .ToListAsync(cancellationToken);

        var results = new List<ReminderTarget>();
        foreach (var c in candidates)
        {
            if (c.MorningReminderHour.HasValue &&
                (c.MorningReminderHour.Value - c.UtcOffsetHours + 24) % 24 == utcHour)
                results.Add(new ReminderTarget(c.UserId, c.Email, c.DailyGoalMinutes, ReminderType.Morning));

            if (c.EveningReminderHour.HasValue &&
                (c.EveningReminderHour.Value - c.UtcOffsetHours + 24) % 24 == utcHour)
                results.Add(new ReminderTarget(c.UserId, c.Email, c.DailyGoalMinutes, ReminderType.Evening));
        }
        return results;
    }
}


