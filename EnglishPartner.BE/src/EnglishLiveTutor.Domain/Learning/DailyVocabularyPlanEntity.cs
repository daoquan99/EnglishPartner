using EnglishLiveTutor.Domain.Common.Primitives;
using EnglishLiveTutor.Domain.Learning.Entities;

namespace EnglishLiveTutor.Domain.Learning;

public sealed class DailyVocabularyPlanEntity : AggregateRoot<Guid>
{
    private List<DailyVocabularyItemEntity> _items = new();

    public string UserId { get; private set; } = "default-learner";
    public string LearningDate { get; private set; } = string.Empty; // yyyy-MM-dd in User TimeZone
    public string TimeZoneId { get; private set; } = "Asia/Ho_Chi_Minh";
    public int NewVocabularyTarget { get; private set; } = 20;
    public string Status { get; private set; } = "Active"; // Active | InProgress | Completed
    public DateTimeOffset? CompletedAt { get; private set; }

    public IReadOnlyCollection<DailyVocabularyItemEntity> Items => _items.AsReadOnly();

    private DailyVocabularyPlanEntity()
    {
    }

    public DailyVocabularyPlanEntity(
        string userId,
        string learningDate,
        string? timeZoneId = "Asia/Ho_Chi_Minh",
        int newVocabularyTarget = 20,
        string status = "InProgress",
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        UserId = string.IsNullOrWhiteSpace(userId) ? "default-learner" : userId.Trim();
        LearningDate = string.IsNullOrWhiteSpace(learningDate) ? ResolveLocalLearningDate(timeZoneId) : learningDate.Trim();
        TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? "Asia/Ho_Chi_Minh" : timeZoneId.Trim();
        NewVocabularyTarget = Math.Clamp(newVocabularyTarget <= 0 ? 20 : newVocabularyTarget, 1, 100);
        Status = string.IsNullOrWhiteSpace(status) ? "InProgress" : status.Trim();
    }

    public void UpdateTarget(int newTarget)
    {
        if (newTarget > 0)
        {
            NewVocabularyTarget = Math.Clamp(newTarget, 1, 100);
        }
    }

    public DailyVocabularyItemEntity AddItem(
        Guid vocabularyId,
        Guid senseId,
        string itemType,
        int? displayOrder = null,
        DateTimeOffset? assignedAt = null,
        Guid? id = null)
    {
        var existing = _items.FirstOrDefault(i => i.SenseId == senseId);
        if (existing is not null)
        {
            return existing;
        }

        int nextOrder = displayOrder ?? (_items.Count == 0 ? 1 : _items.Max(i => i.DisplayOrder) + 1);
        var item = new DailyVocabularyItemEntity(
            planId: Id,
            vocabularyId: vocabularyId,
            senseId: senseId,
            itemType: itemType,
            status: "Pending",
            assignedAt: assignedAt ?? DateTimeOffset.UtcNow,
            displayOrder: nextOrder,
            id: id);

        _items.Add(item);
        return item;
    }

    public void Complete(DateTimeOffset now)
    {
        Status = "Completed";
        CompletedAt = now;
    }

    public static string ResolveLocalLearningDate(string? timeZoneId)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(timeZoneId))
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                var localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
                return localNow.ToString("yyyy-MM-dd");
            }
        }
        catch
        {
            // Fallback to UTC+7 (Asia/Ho_Chi_Minh)
        }
        return DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).ToString("yyyy-MM-dd");
    }
}
