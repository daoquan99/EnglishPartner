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

    private readonly List<Entities.GrammarExample> _examples = [];
    public IReadOnlyList<Entities.GrammarExample> Examples => _examples.AsReadOnly();

    private Grammar() { }

    public static Grammar Create(
        string title,
        string languageCode,
        string? level = null,
        string? explanation = null,
        string? formula = null,
        Guid? topicId = null)
    {
        return new Grammar
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            LanguageCode = languageCode.ToLowerInvariant(),
            Level = level,
            Explanation = explanation,
            Formula = formula,
            TopicId = topicId
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

    public void AddExample(string sentence, string? translation, string? note)
    {
        _examples.Add(Entities.GrammarExample.Create(Id, sentence, translation, note, _examples.Count));
    }

    public void ClearExamples() => _examples.Clear();
}
