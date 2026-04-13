using EnglishPartner.Modules.Curriculum.Contracts.Services;
using EnglishPartner.Modules.Curriculum.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Curriculum.Infrastructure.Services;

/// <summary>
/// Module Interface implementation — allows other modules to query Curriculum content context
/// for AI evaluation without coupling to Curriculum Domain or Application internals.
/// </summary>
internal sealed class CurriculumQueryService(CurriculumDbContext dbContext) : ICurriculumQueryService
{
    public async Task<CurriculumItemContextDto?> GetItemContextAsync(
        Guid itemId, CurriculumItemType itemType, CancellationToken cancellationToken = default)
    {
        return itemType switch
        {
            CurriculumItemType.Vocabulary      => await GetVocabularyContextAsync(itemId, cancellationToken),
            CurriculumItemType.Exercise        => await GetExerciseContextAsync(itemId, cancellationToken),
            CurriculumItemType.ExampleSentence => await GetExampleSentenceContextAsync(itemId, cancellationToken),
            CurriculumItemType.Grammar         => await GetGrammarContextAsync(itemId, cancellationToken),
            _                                  => null
        };
    }

    private async Task<CurriculumItemContextDto?> GetVocabularyContextAsync(Guid id, CancellationToken ct)
    {
        var vocab = await dbContext.Vocabularies
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, ct);

        if (vocab is null) return null;

        var question = $"Define the word \"{vocab.Word}\"" +
                       (vocab.PartOfSpeech is not null ? $" ({vocab.PartOfSpeech})" : "") +
                       (vocab.ExampleSentence is not null ? $"\nExample: {vocab.ExampleSentence}" : "");

        return new CurriculumItemContextDto(
            ItemType: CurriculumItemType.Vocabulary,
            Question: question,
            CorrectAnswer: vocab.Definition,
            Hint: vocab.ExampleTranslation,
            LanguageCode: vocab.LanguageCode);
    }

    private async Task<CurriculumItemContextDto?> GetExerciseContextAsync(Guid id, CancellationToken ct)
    {
        var exercise = await dbContext.Exercises
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (exercise is null) return null;

        var question = exercise.Content is not null
            ? $"{exercise.Instruction}\n{exercise.Content}"
            : exercise.Instruction;

        return new CurriculumItemContextDto(
            ItemType: CurriculumItemType.Exercise,
            Question: question,
            CorrectAnswer: exercise.CorrectAnswer,
            Hint: exercise.Explanation,
            LanguageCode: exercise.LanguageCode);
    }

    private async Task<CurriculumItemContextDto?> GetExampleSentenceContextAsync(Guid id, CancellationToken ct)
    {
        var sentence = await dbContext.ExampleSentences
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (sentence is null) return null;

        return new CurriculumItemContextDto(
            ItemType: CurriculumItemType.ExampleSentence,
            Question: $"Translate this sentence to Vietnamese:\n\"{sentence.Text}\"",
            CorrectAnswer: sentence.Translation ?? sentence.Text,
            Hint: sentence.Note,
            LanguageCode: sentence.LanguageCode);
    }

    private async Task<CurriculumItemContextDto?> GetGrammarContextAsync(Guid id, CancellationToken ct)
    {
        var grammar = await dbContext.Grammars
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id, ct);

        if (grammar is null) return null;

        var question = grammar.Explanation is not null
            ? $"Explain and apply the grammar rule: \"{grammar.Title}\"\nRule: {grammar.Explanation}"
            : $"Explain the grammar rule: \"{grammar.Title}\"";

        return new CurriculumItemContextDto(
            ItemType: CurriculumItemType.Grammar,
            Question: question,
            CorrectAnswer: grammar.Explanation ?? grammar.Title,
            Hint: grammar.Formula,
            LanguageCode: grammar.LanguageCode);
    }
}
