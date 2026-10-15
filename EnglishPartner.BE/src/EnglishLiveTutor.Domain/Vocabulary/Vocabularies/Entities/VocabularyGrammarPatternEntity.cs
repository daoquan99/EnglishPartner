using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Vocabulary.Vocabularies.Entities;

public sealed class VocabularyGrammarPatternEntity : Entity<Guid>
{
    public Guid SenseId { get; private set; }
    public string Pattern { get; private set; } = string.Empty;
    public string? Explanation { get; private set; }
    public string? Example { get; private set; }

    private VocabularyGrammarPatternEntity()
    {
    }

    internal VocabularyGrammarPatternEntity(
        Guid senseId,
        string pattern,
        string? explanation = null,
        string? example = null,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new InvalidOperationException("Pattern không được để trống.");

        SenseId = senseId;
        Pattern = pattern.Trim();
        Explanation = string.IsNullOrWhiteSpace(explanation) ? null : explanation.Trim();
        Example = string.IsNullOrWhiteSpace(example) ? null : example.Trim();
    }
}
