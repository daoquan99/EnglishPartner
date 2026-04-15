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
            CurriculumItemType.Conversation    => await GetConversationContextAsync(itemId, cancellationToken),
            CurriculumItemType.Topic           => await GetTopicContextAsync(itemId, cancellationToken),
            _                                  => null
        };
    }

    private async Task<CurriculumItemContextDto?> GetVocabularyContextAsync(Guid id, CancellationToken ct)
    {
        var vocab = await dbContext.Vocabularies
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, ct);

        if (vocab is null) 
            return null;

        // Grab the first linked example sentence (if any)
        var example = await dbContext.ExampleSentences
            .AsNoTracking()
            .Include(s => s.Translations)
            .Where(s => s.VocabularyId == id)
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(ct);

        var exampleText = example?.Text;
        var exampleTranslation = example?.Translations.FirstOrDefault(t => t.LanguageCode == "vi")?.TranslatedText;

        var question = $"Define the word \"{vocab.Word}\"" +
                       (vocab.PartOfSpeech is not null ? $" ({vocab.PartOfSpeech})" : "") +
                       (exampleText is not null ? $"\nExample: {exampleText}" : "");

        return new CurriculumItemContextDto(
            ItemType: CurriculumItemType.Vocabulary,
            Question: question,
            CorrectAnswer: vocab.Definition,
            Hint: exampleTranslation,
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
            .Include(s => s.Translations)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (sentence is null) return null;

        var correctAnswer = sentence.Translations.FirstOrDefault(t => t.LanguageCode == "vi")?.TranslatedText
                            ?? sentence.Text;

        return new CurriculumItemContextDto(
            ItemType: CurriculumItemType.ExampleSentence,
            Question: $"Translate this sentence to Vietnamese:\n\"{sentence.Text}\"",
            CorrectAnswer: correctAnswer,
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

    private async Task<CurriculumItemContextDto?> GetConversationContextAsync(Guid id, CancellationToken ct)
    {
        var conversation = await dbContext.Conversations
            .AsNoTracking()
            .Include(c => c.Lines)
                .ThenInclude(l => l.Translations)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (conversation is null) return null;

        var firstLine = conversation.Lines.OrderBy(l => l.SortOrder).FirstOrDefault();
        if (firstLine is null)
            return new CurriculumItemContextDto(
                ItemType: CurriculumItemType.Conversation,
                Question: $"Practice this conversation: \"{conversation.Title}\"",
                CorrectAnswer: conversation.Title,
                Hint: conversation.Scenario,
                LanguageCode: conversation.LanguageCode);

        var translation = firstLine.Translations.FirstOrDefault(t => t.LanguageCode == "vi")?.TranslatedText;
        return new CurriculumItemContextDto(
            ItemType: CurriculumItemType.Conversation,
            Question: $"Translate this line from the conversation \"{conversation.Title}\":\n\"{firstLine.Text}\"",
            CorrectAnswer: translation ?? firstLine.Text,
            Hint: $"Speaker: {firstLine.Speaker}",
            LanguageCode: conversation.LanguageCode);
    }

    private async Task<CurriculumItemContextDto?> GetTopicContextAsync(Guid id, CancellationToken ct)
    {
        var topic = await dbContext.Topics
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (topic is null) return null;

        var description = topic.Description is not null
            ? $"\nContext: {topic.Description}"
            : string.Empty;

        return new CurriculumItemContextDto(
            ItemType: CurriculumItemType.Topic,
            Question: $"Speak or write about the topic: \"{topic.Title}\"{description}",
            CorrectAnswer: topic.Title,
            Hint: topic.Description,
            LanguageCode: topic.LanguageCode);
    }
}
