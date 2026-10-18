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
    public string? TargetCefrLevel { get; private set; }
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
        string? targetCefrLevel = null,
        string status = "InProgress",
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId không được để trống.", nameof(userId));
        if (newVocabularyTarget < 1 || newVocabularyTarget > 100)
            throw new ArgumentOutOfRangeException(nameof(newVocabularyTarget), newVocabularyTarget, "Mục tiêu học từ vựng mỗi ngày phải nằm trong khoảng từ 1 đến 100 từ.");

        UserId = userId.Trim();
        LearningDate = string.IsNullOrWhiteSpace(learningDate) ? ResolveLocalLearningDate(timeZoneId) : learningDate.Trim();
        TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? "Asia/Ho_Chi_Minh" : timeZoneId.Trim();
        NewVocabularyTarget = newVocabularyTarget;
        TargetCefrLevel = string.IsNullOrWhiteSpace(targetCefrLevel) ? null : targetCefrLevel.Trim().ToUpperInvariant();
        Status = string.IsNullOrWhiteSpace(status) ? "InProgress" : status.Trim();
    }

    public void UpdateTarget(int newTarget)
    {
        if (newTarget < 1 || newTarget > 100)
            throw new ArgumentOutOfRangeException(nameof(newTarget), newTarget, "Mục tiêu học từ vựng mỗi ngày phải nằm trong khoảng từ 1 đến 100 từ.");

        NewVocabularyTarget = newTarget;
    }

    public DailyVocabularyItemEntity AddItem(
        Guid vocabularyId,
        Guid senseId,
        string itemType,
        int? displayOrder = null,
        DateTimeOffset? assignedAt = null,
        Guid? id = null)
    {
        var existing = _items.FirstOrDefault(i => i.VocabularyId == vocabularyId || i.SenseId == senseId);
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

        if (Status == "Completed")
        {
            Status = "InProgress";
            CompletedAt = null;
        }

        return item;
    }

    public DailyVocabularyItemEntity CompleteItem(Guid itemId, DateTimeOffset now)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            throw new InvalidOperationException($"Mục học tập '{itemId}' không thuộc về kế hoạch '{Id}'.");

        item.Complete(now);

        if (_items.Count > 0 && _items.All(i => i.Status == "Completed"))
        {
            Status = "Completed";
            CompletedAt = now;
        }
        else
        {
            Status = "InProgress";
        }

        return item;
    }

    public void Complete(DateTimeOffset now)
    {
        Status = "Completed";
        CompletedAt = now;
    }

    public static string ResolveLocalLearningDate(string? timeZoneId, TimeProvider? timeProvider = null)
    {
        var now = (timeProvider ?? TimeProvider.System).GetUtcNow();
        try
        {
            if (!string.IsNullOrWhiteSpace(timeZoneId))
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                var localNow = TimeZoneInfo.ConvertTime(now, tz);
                return localNow.ToString("yyyy-MM-dd");
            }
        }
        catch
        {
            // Fallback to UTC+7 (Asia/Ho_Chi_Minh)
        }
        return now.ToOffset(TimeSpan.FromHours(7)).ToString("yyyy-MM-dd");
    }
}
