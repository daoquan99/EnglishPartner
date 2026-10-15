using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Topics;

/// <summary>
/// Aggregate Root representing a curated or custom learning Topic for Live AI Tutor
/// conversations and AI Vocabulary Generation.
/// </summary>
public sealed class TopicEntity : AggregateRoot<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string DisplayNameVi { get; private set; } = string.Empty;
    public string Category { get; private set; } = "General";
    public string? ModeCode { get; private set; }
    public string SuggestedCefrLevel { get; private set; } = "B1";
    public bool SupportsConversation { get; private set; } = true;
    public bool SupportsVocabulary { get; private set; } = true;
    public string Description { get; private set; } = string.Empty;
    public bool IsFeatured { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int DisplayOrder { get; private set; } = 100;

    private TopicEntity()
    {
    }

    public TopicEntity(
        string code,
        string name,
        string displayNameVi,
        string category,
        string? modeCode = null,
        string suggestedCefrLevel = "B1",
        bool supportsConversation = true,
        bool supportsVocabulary = true,
        string description = "",
        bool isFeatured = false,
        bool isActive = true,
        int displayOrder = 100,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Topic code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Topic name is required.", nameof(name));

        Code = code.Trim().ToLowerInvariant();
        Name = name.Trim();
        DisplayNameVi = string.IsNullOrWhiteSpace(displayNameVi) ? Name : displayNameVi.Trim();
        Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();
        ModeCode = string.IsNullOrWhiteSpace(modeCode) ? null : modeCode.Trim().ToLowerInvariant();
        SuggestedCefrLevel = string.IsNullOrWhiteSpace(suggestedCefrLevel) ? "B1" : suggestedCefrLevel.Trim().ToUpperInvariant();
        SupportsConversation = supportsConversation;
        SupportsVocabulary = supportsVocabulary;
        Description = description?.Trim() ?? string.Empty;
        IsFeatured = isFeatured;
        IsActive = isActive;
        DisplayOrder = displayOrder;
    }

    public void UpdateDetails(
        string name,
        string displayNameVi,
        string category,
        string? modeCode,
        string suggestedCefrLevel,
        bool supportsConversation,
        bool supportsVocabulary,
        string description,
        bool isFeatured,
        bool isActive,
        int displayOrder)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name.Trim();
        if (!string.IsNullOrWhiteSpace(displayNameVi))
            DisplayNameVi = displayNameVi.Trim();
        if (!string.IsNullOrWhiteSpace(category))
            Category = category.Trim();

        ModeCode = string.IsNullOrWhiteSpace(modeCode) ? null : modeCode.Trim().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(suggestedCefrLevel))
            SuggestedCefrLevel = suggestedCefrLevel.Trim().ToUpperInvariant();

        SupportsConversation = supportsConversation;
        SupportsVocabulary = supportsVocabulary;
        Description = description?.Trim() ?? string.Empty;
        IsFeatured = isFeatured;
        IsActive = isActive;
        DisplayOrder = displayOrder;
    }

    public override void SoftDelete(DateTimeOffset deletedAtUtc, string? deletedBy = null)
    {
        base.SoftDelete(deletedAtUtc, deletedBy);
        IsActive = false;
    }
}
