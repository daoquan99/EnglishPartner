using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Curriculum.Domain.Conversations.Entities;

/// <summary>
/// A single line spoken within a conversation.
/// </summary>
public sealed class ConversationLine : Entity
{
    public Guid ConversationId { get; private set; }
    public string Speaker { get; private set; } = null!;
    public string Text { get; private set; } = null!;
    public string? Translation { get; private set; }
    public string? AudioUrl { get; private set; }
    public int SortOrder { get; private set; }

    private ConversationLine() { }

    public static ConversationLine Create(Guid conversationId, string speaker, string text, string? translation, string? audioUrl, int sortOrder)
    {
        return new ConversationLine
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Speaker = speaker.Trim(),
            Text = text.Trim(),
            Translation = translation?.Trim(),
            AudioUrl = audioUrl,
            SortOrder = sortOrder
        };
    }
}
