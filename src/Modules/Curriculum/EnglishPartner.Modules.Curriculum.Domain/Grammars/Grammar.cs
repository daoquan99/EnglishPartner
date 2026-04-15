using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Curriculum.Domain.Grammars;

/// <summary>
/// A grammar rule/concept with explanations and examples.
/// Examples: "Present Simple", "Passive Voice", "Conditionals".
/// </summary>
public sealed class Grammar : AggregateRoot
{
    public string Title { get; private set; } = null!;
    public string LanguageCode { get; private set; } = "en";
    public string? Level { get; private set; }
    public string? Explanation { get; private set; }
    public string? Formula { get; private set; }
    public Guid? TopicId { get; private set; }
    /// <summary>True when this entry was created by AI generation, not manually entered.</summary>
    public bool IsAiGenerated { get; private set; }

    private readonly List<Entities.GrammarExample> _examples = [];
    public IReadOnlyList<Entities.GrammarExample> Examples => _examples.AsReadOnly();

    private Grammar() { }

    public static Grammar Create(
        string title,
        string languageCode,
        string? level = null,
        string? explanation = null,
        string? formula = null,
        Guid? topicId = null,
        bool isAiGenerated = false)
    {
        return new Grammar
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            LanguageCode = languageCode.ToLowerInvariant(),
            Level = level,
            Explanation = explanation,
            Formula = formula,
            TopicId = topicId,
            IsAiGenerated = isAiGenerated
        };
    }

    public void Update(string title, string? level, string? explanation, string? formula, Guid? topicId)
    {
        Title = title.Trim();
        Level = level;
        Explanation = explanation;
        Formula = formula;
        TopicId = topicId;
    }

    /// <summary>
    /// Adds a new example sentence. Use example.AddTranslation() to attach translations.
    /// </summary>
    public Entities.GrammarExample AddExample(string sentence, string? note)
    {
        var example = Entities.GrammarExample.Create(Id, sentence, note, _examples.Count);
        _examples.Add(example);
        return example;
    }

    public void ClearExamples() => _examples.Clear();
}
