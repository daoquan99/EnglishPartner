using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Curriculum.Domain.Conversations.Entities;

/// <summary>
/// A translation of a conversation line into a specific target language.
/// </summary>
public sealed class ConversationLineTranslation : Entity
{
    public Guid ConversationLineId { get; private set; }

    /// <summary>
    /// BCP-47 language code of the translation, e.g. "vi", "ja", "ko".
    /// </summary>
    public string LanguageCode { get; private set; } = null!;

    public string TranslatedText { get; private set; } = null!;

    private ConversationLineTranslation() { }

    public static ConversationLineTranslation Create(Guid conversationLineId, string languageCode, string translatedText)
    {
        return new ConversationLineTranslation
        {
            Id = Guid.NewGuid(),
            ConversationLineId = conversationLineId,
            LanguageCode = languageCode.ToLowerInvariant(),
            TranslatedText = translatedText.Trim()
        };
    }
}
