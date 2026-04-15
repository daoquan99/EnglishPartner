using EnglishPartner.Modules.AI.Application.Abstractions;
using EnglishPartner.Modules.AI.Contracts.Services;
using EnglishPartner.Modules.AI.Contracts.Services.Dtos;

namespace EnglishPartner.Modules.AI.Infrastructure.Services;

/// <summary>
/// Implements cross-module content generation contract.
/// Translates between IGeminiService Gemini-specific records and the public Dtos in AI.Contracts.
/// </summary>
internal sealed class CurriculumContentGenerationService(IGeminiService geminiService) : ICurriculumContentGenerationService
{
    public async Task<List<GeneratedVocabularyDto>> GenerateVocabularyAsync(
        string wordHint, int count = 1, CancellationToken cancellationToken = default)
    {
        var results = await geminiService.GenerateVocabularyAsync(wordHint, count, cancellationToken);
        return results.Select(r => new GeneratedVocabularyDto(
            r.Word, r.Definition, r.Pronunciation, r.PhoneticTranscription,
            r.PartOfSpeech, r.Level, r.Synonyms)).ToList();
    }

    public async Task<List<GeneratedGrammarDto>> GenerateGrammarAsync(
        string title, string? level, int exampleCount,
        List<string>? targetLanguageCodes = null, int count = 1,
        CancellationToken cancellationToken = default)
    {
        var results = await geminiService.GenerateGrammarAsync(title, level, exampleCount, targetLanguageCodes, count, cancellationToken);
        return results.Select(r =>
        {
            var examples = r.Examples
                .Select(e => new GeneratedGrammarExampleDto(e.Sentence, e.Note, e.Translations))
                .ToList();
            return new GeneratedGrammarDto(r.Title, r.Explanation, r.Formula, r.Level, examples);
        }).ToList();
    }

    public async Task<List<GeneratedConversationContentDto>> GenerateConversationAsync(
        string topicName, string? level, string? scenario, int lineCount,
        List<string>? targetLanguageCodes = null, int count = 1,
        CancellationToken cancellationToken = default)
    {
        var results = await geminiService.GenerateFullConversationAsync(topicName, level, scenario, lineCount, targetLanguageCodes, count, cancellationToken);
        return results.Select(r =>
        {
            var lines = r.Lines
                .Select(l => new GeneratedConversationLineDto(l.Speaker, l.Text, l.Translations))
                .ToList();
            return new GeneratedConversationContentDto(r.Title, lines);
        }).ToList();
    }

    public async Task<List<GeneratedExerciseDto>> GenerateExerciseAsync(
        string context, int exerciseType, string? level, int count = 1,
        CancellationToken cancellationToken = default)
    {
        var results = await geminiService.GenerateExerciseAsync(context, exerciseType, level, count, cancellationToken);
        return results.Select(r =>
        {
            var options = r.Options
                .Select(o => new GeneratedExerciseOptionDto(o.Text, o.IsCorrect))
                .ToList();
            return new GeneratedExerciseDto(r.Title, r.Instruction, r.Content, r.CorrectAnswer, r.Explanation, options);
        }).ToList();
    }

    public async Task<List<GeneratedExampleSentenceDto>> GenerateExampleSentencesAsync(
        string context, int count, string? level,
        List<string>? targetLanguageCodes = null,
        CancellationToken cancellationToken = default)
    {
        var results = await geminiService.GenerateExampleSentencesAsync(context, count, level, targetLanguageCodes, cancellationToken);
        return results.Select(s => new GeneratedExampleSentenceDto(s.Text, s.Note, s.Translations)).ToList();
    }
}
