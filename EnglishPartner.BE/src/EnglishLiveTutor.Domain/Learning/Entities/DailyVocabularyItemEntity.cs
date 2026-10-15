using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Learning.Entities;

public sealed class DailyVocabularyItemEntity : Entity<Guid>
{
    public Guid PlanId { get; private set; }
    public Guid VocabularyId { get; private set; }
    public Guid SenseId { get; private set; }
    public string ItemType { get; private set; } = "New"; // New | Review | CarryOver
    public string Status { get; private set; } = "Pending"; // Pending | InProgress | Completed
    public DateTimeOffset AssignedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; private set; }
    public int DisplayOrder { get; private set; }

    private DailyVocabularyItemEntity()
    {
    }

    public DailyVocabularyItemEntity(
        Guid planId,
        Guid vocabularyId,
        Guid senseId,
        string itemType = "New",
        string status = "Pending",
        DateTimeOffset? assignedAt = null,
        int displayOrder = 1,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        PlanId = planId;
        VocabularyId = vocabularyId;
        SenseId = senseId;
        ItemType = string.IsNullOrWhiteSpace(itemType) ? "New" : itemType.Trim();
        Status = string.IsNullOrWhiteSpace(status) ? "Pending" : status.Trim();
        AssignedAt = assignedAt ?? DateTimeOffset.UtcNow;
        DisplayOrder = displayOrder <= 0 ? 1 : displayOrder;
    }

    public void Complete(DateTimeOffset now)
    {
        Status = "Completed";
        CompletedAt = now;
    }
}
