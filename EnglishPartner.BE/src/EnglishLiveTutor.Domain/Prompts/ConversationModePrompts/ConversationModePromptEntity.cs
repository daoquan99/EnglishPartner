using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Prompts.ConversationModePrompts;

/// <summary>
/// Aggregate Root storing each of the 19 Conversation & Coaching Mode directives for CefrPromptBuilder.
/// </summary>
public sealed class ConversationModePromptEntity : AggregateRoot<Guid>
{
    public string ModeCode { get; private set; } = "daily_conversation";
    public string Category { get; private set; } = "Communication & Workplace"; // Interview & Coaching | Communication & Workplace
    public string DisplayName { get; private set; } = string.Empty;
    public string DirectiveTemplate { get; private set; } = string.Empty; // Supports {cefrLevel} placeholder
    public int DisplayOrder { get; private set; } = 1;
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; } = true;

    private ConversationModePromptEntity()
    {
    }

    public ConversationModePromptEntity(
        string modeCode,
        string category,
        string displayName,
        string directiveTemplate,
        int displayOrder = 1,
        bool isDefault = false,
        bool isActive = true,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(modeCode))
            throw new InvalidOperationException("ModeCode không được để trống.");
        if (string.IsNullOrWhiteSpace(directiveTemplate))
            throw new InvalidOperationException("DirectiveTemplate không được để trống.");

        ModeCode = modeCode.Trim().ToLowerInvariant();
        Category = string.IsNullOrWhiteSpace(category) ? "Communication & Workplace" : category.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? ModeCode : displayName.Trim();
        DirectiveTemplate = directiveTemplate.Trim();
        DisplayOrder = displayOrder <= 0 ? 1 : displayOrder;
        IsDefault = isDefault;
        IsActive = isActive;
    }

    public string RenderDirective(string cefrLevel)
    {
        string normalizedCefr = string.IsNullOrWhiteSpace(cefrLevel) ? "B1" : cefrLevel.Trim().ToUpperInvariant();
        return DirectiveTemplate.Replace("{cefrLevel}", normalizedCefr, StringComparison.OrdinalIgnoreCase);
    }

    public void UpdateModePrompt(
        string category,
        string displayName,
        string directiveTemplate,
        int displayOrder,
        bool isDefault,
        bool isActive)
    {
        if (!string.IsNullOrWhiteSpace(category))
            Category = category.Trim();
        if (!string.IsNullOrWhiteSpace(displayName))
            DisplayName = displayName.Trim();
        if (!string.IsNullOrWhiteSpace(directiveTemplate))
            DirectiveTemplate = directiveTemplate.Trim();
        if (displayOrder > 0)
            DisplayOrder = displayOrder;
        IsDefault = isDefault;
        IsActive = isActive;
    }
}
