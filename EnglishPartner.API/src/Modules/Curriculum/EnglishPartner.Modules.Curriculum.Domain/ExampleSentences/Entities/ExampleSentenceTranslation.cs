using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Curriculum.Domain.ExampleSentences.Entities;

/// <summary>
/// A translation of an example sentence into a specific target language.
/// </summary>
public sealed class ExampleSentenceTranslation : Entity
{
    public Guid ExampleSentenceId { get; private set; }

    /// <summary>
    /// BCP-47 language code of the translation, e.g. "vi", "ja", "ko".
    /// </summary>
    public string LanguageCode { get; private set; } = null!;

    public string TranslatedText { get; private set; } = null!;

    private ExampleSentenceTranslation() { }

    public static ExampleSentenceTranslation Create(Guid exampleSentenceId, string languageCode, string translatedText)
    {
        return new ExampleSentenceTranslation
        {
            Id = Guid.NewGuid(),
            ExampleSentenceId = exampleSentenceId,
            LanguageCode = languageCode.ToLowerInvariant(),
            TranslatedText = translatedText.Trim()
        };
    }
}
