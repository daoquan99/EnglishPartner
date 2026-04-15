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
    public string? AudioUrl { get; private set; }
    public int SortOrder { get; private set; }

    private readonly List<ConversationLineTranslation> _translations = [];
    public IReadOnlyList<ConversationLineTranslation> Translations => _translations.AsReadOnly();

    private ConversationLine() { }

    public static ConversationLine Create(Guid conversationId, string speaker, string text, string? audioUrl, int sortOrder)
    {
        return new ConversationLine
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Speaker = speaker.Trim(),
            Text = text.Trim(),
            AudioUrl = audioUrl,
            SortOrder = sortOrder
        };
    }

    public void AddTranslation(string languageCode, string translatedText)
    {
        var existing = _translations.FirstOrDefault(t => t.LanguageCode == languageCode);
        if (existing is not null) _translations.Remove(existing);
        _translations.Add(ConversationLineTranslation.Create(Id, languageCode, translatedText));
    }
}
