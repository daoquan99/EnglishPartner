using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Curriculum.Domain.Grammars.Entities;

/// <summary>
/// A translation of a grammar example sentence into a specific target language.
/// </summary>
public sealed class GrammarExampleTranslation : Entity
{
    public Guid GrammarExampleId { get; private set; }

    /// <summary>
    /// BCP-47 language code of the translation, e.g. "vi", "ja", "ko".
    /// </summary>
    public string LanguageCode { get; private set; } = null!;

    public string TranslatedText { get; private set; } = null!;

    private GrammarExampleTranslation() { }

    public static GrammarExampleTranslation Create(Guid grammarExampleId, string languageCode, string translatedText)
    {
        return new GrammarExampleTranslation
        {
            Id = Guid.NewGuid(),
            GrammarExampleId = grammarExampleId,
            LanguageCode = languageCode.ToLowerInvariant(),
            TranslatedText = translatedText.Trim()
        };
    }
}
