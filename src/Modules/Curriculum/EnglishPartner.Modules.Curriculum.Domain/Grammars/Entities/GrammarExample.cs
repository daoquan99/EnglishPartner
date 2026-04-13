using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Curriculum.Domain.Grammars.Entities;

/// <summary>
/// An example sentence demonstrating a grammar rule.
/// </summary>
public sealed class GrammarExample : Entity
{
    public Guid GrammarId { get; private set; }
    public string Sentence { get; private set; } = null!;
    public string? Translation { get; private set; }
    public string? Note { get; private set; }
    public int SortOrder { get; private set; }

    private GrammarExample() { }

    public static GrammarExample Create(Guid grammarId, string sentence, string? translation, string? note, int sortOrder)
    {
        return new GrammarExample
        {
            Id = Guid.NewGuid(),
            GrammarId = grammarId,
            Sentence = sentence.Trim(),
            Translation = translation?.Trim(),
            Note = note?.Trim(),
            SortOrder = sortOrder
        };
    }
}
