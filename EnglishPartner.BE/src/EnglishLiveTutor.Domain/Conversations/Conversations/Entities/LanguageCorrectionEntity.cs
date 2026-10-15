using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Conversations.Conversations.Entities;

public sealed class LanguageCorrectionEntity : Entity<string>
{
    public string ConversationId { get; private set; } = string.Empty;
    public string TurnId { get; private set; } = string.Empty;
    public string OriginalText { get; private set; } = string.Empty;
    public string CorrectedText { get; private set; } = string.Empty;
    public string ErrorCategory { get; private set; } = "Grammar";
    public string ExplanationVi { get; private set; } = string.Empty;
    public string CefrTip { get; private set; } = string.Empty;

    private LanguageCorrectionEntity()
        : base(Guid.NewGuid().ToString("N")[..12])
    {
    }

    public LanguageCorrectionEntity(
        string? id,
        string conversationId,
        string turnId,
        string originalText,
        string correctedText,
        string errorCategory,
        string explanationVi,
        string cefrTip,
        DateTimeOffset? createdAtUtc = null)
        : base(string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N")[..12] : id)
    {
        ConversationId = conversationId;
        TurnId = turnId ?? string.Empty;
        OriginalText = originalText ?? string.Empty;
        CorrectedText = correctedText ?? string.Empty;
        ErrorCategory = string.IsNullOrWhiteSpace(errorCategory) ? "Grammar" : errorCategory.Trim();
        ExplanationVi = explanationVi ?? string.Empty;
        CefrTip = cefrTip ?? string.Empty;
    }
}
