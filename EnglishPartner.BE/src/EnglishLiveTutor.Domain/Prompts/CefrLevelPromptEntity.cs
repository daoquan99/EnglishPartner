using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Prompts;

/// <summary>
/// Aggregate Root storing CEFR level adaptation guidance (A1..C2) and system prompt rules for CefrPromptBuilder.
/// </summary>
public sealed class CefrLevelPromptEntity : AggregateRoot<Guid>
{
    public string CefrLevel { get; private set; } = "B1"; // A1 | A2 | B1 | B2 | C1 | C2
    public string DisplayName { get; private set; } = string.Empty;
    public string LevelGuidance { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; } = 1;
    public bool IsActive { get; private set; } = true;

    private CefrLevelPromptEntity()
    {
    }

    public CefrLevelPromptEntity(
        string cefrLevel,
        string displayName,
        string levelGuidance,
        int displayOrder = 1,
        bool isActive = true,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(cefrLevel))
            throw new InvalidOperationException("CefrLevel không được để trống.");
        if (string.IsNullOrWhiteSpace(levelGuidance))
            throw new InvalidOperationException("LevelGuidance không được để trống.");

        CefrLevel = cefrLevel.Trim().ToUpperInvariant();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? CefrLevel : displayName.Trim();
        LevelGuidance = levelGuidance.Trim();
        DisplayOrder = displayOrder <= 0 ? 1 : displayOrder;
        IsActive = isActive;
    }

    public void UpdateGuidance(string displayName, string levelGuidance, int displayOrder, bool isActive = true)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
            DisplayName = displayName.Trim();
        if (!string.IsNullOrWhiteSpace(levelGuidance))
            LevelGuidance = levelGuidance.Trim();
        if (displayOrder > 0)
            DisplayOrder = displayOrder;
        IsActive = isActive;
    }
}
