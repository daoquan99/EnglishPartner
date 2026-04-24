namespace EnglishPartner.Modules.Learning.Domain.UserProfiles.Repositories;

public interface IUserLearningProfileRepository
{
    Task<UserLearningProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserLearningProfile>> GetLeaderboardAsync(int topCount, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReminderTarget>> GetUsersForReminderAsync(int utcHour, CancellationToken cancellationToken = default);
    void Add(UserLearningProfile profile);
    void Update(UserLearningProfile profile);
}

/// <summary>Lightweight projection used by DailyGoalReminderJob.</summary>
public sealed record ReminderTarget(
    Guid UserId,
    string Email,
    int DailyGoalMinutes,
    ReminderType ReminderType);

public enum ReminderType { Morning, Evening }
