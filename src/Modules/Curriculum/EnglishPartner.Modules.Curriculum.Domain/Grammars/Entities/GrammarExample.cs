using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Curriculum.Domain.Grammars.Entities;

/// <summary>
/// An example sentence demonstrating a grammar rule.
/// </summary>
public sealed class GrammarExample : Entity
{
    public Guid GrammarId { get; private set; }
    public string Sentence { get; private set; } = null!;
    public string? Note { get; private set; }
    public int SortOrder { get; private set; }

    private readonly List<GrammarExampleTranslation> _translations = [];
    public IReadOnlyList<GrammarExampleTranslation> Translations => _translations.AsReadOnly();

    private GrammarExample() { }

    public static GrammarExample Create(Guid grammarId, string sentence, string? note, int sortOrder)
    {
        return new GrammarExample
        {
            Id = Guid.NewGuid(),
            GrammarId = grammarId,
            Sentence = sentence.Trim(),
            Note = note?.Trim(),
            SortOrder = sortOrder
        };
    }

    public void AddTranslation(string languageCode, string translatedText)
    {
        var existing = _translations.FirstOrDefault(t => t.LanguageCode == languageCode);
        if (existing is not null) _translations.Remove(existing);
        _translations.Add(GrammarExampleTranslation.Create(Id, languageCode, translatedText));
    }
}
