using EnglishLiveTutor.Domain.Common.Primitives;
using EnglishLiveTutor.Domain.Learning.DailyVocabularyPlans.Entities;

namespace EnglishLiveTutor.Domain.Learning.DailyVocabularyPlans;

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

    public void EnsureOwnedBy(string userId)
    {
        string normalizedCaller = string.IsNullOrWhiteSpace(userId) ? "default-learner" : userId.Trim();
        if (!string.Equals(UserId, normalizedCaller, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Bạn không có quyền thao tác trên kế hoạch học tập của người dùng khác.");
        }
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

        if (string.Equals(Status, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            Status = "InProgress";
            CompletedAt = null;
        }

        return item;
    }

    public DailyVocabularyItemEntity CompleteItem(Guid itemId, DateTimeOffset nowUtc)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new InvalidOperationException("Không tìm thấy mục từ vựng trong kế hoạch học tập hôm nay.");

        item.Complete(nowUtc);
        ReconcileCompletionStatus(nowUtc);
        return item;
    }

    public void ReconcileCompletionStatus(DateTimeOffset nowUtc)
    {
        if (_items.Count > 0 && _items.All(i => string.Equals(i.Status, "Completed", StringComparison.OrdinalIgnoreCase)))
        {
            Complete(nowUtc);
        }
        else if (string.Equals(Status, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            Status = "InProgress";
            CompletedAt = null;
        }
    }

    public void Complete(DateTimeOffset now)
    {
        Status = "Completed";
        CompletedAt = now;
    }

    public static string ResolveLocalLearningDate(string? timeZoneId, DateTimeOffset? utcNow = null)
    {
        var referenceUtc = utcNow ?? DateTimeOffset.UtcNow;
        try
        {
            if (!string.IsNullOrWhiteSpace(timeZoneId))
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                var localNow = TimeZoneInfo.ConvertTime(referenceUtc, tz);
                return localNow.ToString("yyyy-MM-dd");
            }
        }
        catch
        {
            // Fallback to UTC+7 (Asia/Ho_Chi_Minh)
        }
        return referenceUtc.ToOffset(TimeSpan.FromHours(7)).ToString("yyyy-MM-dd");
    }
}
