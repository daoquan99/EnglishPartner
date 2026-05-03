using EnglishPartner.Modules.AI.Application.Abstractions;
using EnglishPartner.Modules.AI.Contracts.Services;
using EnglishPartner.Modules.AI.Contracts.Services.Dtos;

namespace EnglishPartner.Modules.AI.Infrastructure.Services;

/// <summary>
/// Implements cross-module content generation contract.
/// Translates between IGeminiService Gemini-specific records and the public Dtos in AI.Contracts.
/// </summary>
internal sealed class CurriculumContentGenerationService(IGeminiService geminiService) 
    : ICurriculumContentGenerationService
{
    public async Task<List<GeneratedVocabularyDto>> GenerateVocabularyAsync(
        string targetLanguage, string wordHint, int count = 1, CancellationToken cancellationToken = default)
    {
        var results = await geminiService.GenerateVocabularyAsync(
            targetLanguage: targetLanguage,
            wordHint: wordHint,
            count: count,
            cancellationToken: cancellationToken);
        return results.Select(r => new GeneratedVocabularyDto(
            Word: r.Word,
            Definition: r.Definition,
            Pronunciation: r.Pronunciation,
            PhoneticTranscription: r.PhoneticTranscription,
            PartOfSpeech: r.PartOfSpeech,
            Level: r.Level,
            Synonyms: r.Synonyms))
            .ToList();
    }

    public async Task<List<GeneratedGrammarDto>> GenerateGrammarAsync(
        string targetLanguage,
        string title,
        string? level,
        int exampleCount,
        List<string>? targetLanguageCodes = null,
        int count = 1,
        CancellationToken cancellationToken = default)
    {
        var results = await geminiService.GenerateGrammarAsync(targetLanguage, title, level, exampleCount, targetLanguageCodes, count, cancellationToken);
        return results.Select(r =>
        {
            var examples = r.Examples
                .Select(e => new GeneratedGrammarExampleDto(e.Sentence, e.Note, e.Translations))
                .ToList();
            return new GeneratedGrammarDto(r.Title, r.Explanation, r.Formula, r.Level, examples);
        }).ToList();
    }

    public async Task<List<GeneratedConversationContentDto>> GenerateConversationAsync(
        string targetLanguage,
        string topicName,
        string? level,
        string? scenario,
        int lineCount,
        List<string>? targetLanguageCodes = null,
        int count = 1,
        CancellationToken cancellationToken = default)
    {
        var results = await geminiService.GenerateFullConversationAsync(
            targetLanguage: targetLanguage,
            topicName: topicName,
            level: level,
            scenario: scenario,
            lineCount: lineCount,
            targetLanguageCodes: targetLanguageCodes,
            count: count,
            cancellationToken: cancellationToken);
        return results.Select(r =>
        {
            var lines = r.Lines
                .Select(l => new GeneratedConversationLineDto(l.Speaker, l.Text, l.Translations))
                .ToList();
            return new GeneratedConversationContentDto(r.Title, lines);
        }).ToList();
    }

    public async Task<List<GeneratedExerciseDto>> GenerateExerciseAsync(
        string targetLanguage,
        string context,
        int exerciseType,
        string? level,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        var results = await geminiService.GenerateExerciseAsync(
            targetLanguage: targetLanguage,
            context: context,
            exerciseType: exerciseType,
            level: level,
            count: count,
            cancellationToken: cancellationToken);
        return results.Select(r =>
        {
            var options = r.Options
                .Select(o => new GeneratedExerciseOptionDto(o.Text, o.IsCorrect))
                .ToList();
            return new GeneratedExerciseDto(r.Title, r.Instruction, r.Content, r.CorrectAnswer, r.Explanation, options);
        }).ToList();
    }

    public async Task<List<GeneratedExerciseOptionDto>> GenerateExerciseOptionsAsync(
        string targetLanguage,
        int exerciseType,
        string title,
        string instruction,
        string? content,
        string correctAnswer,
        int optionCount = 4,
        CancellationToken cancellationToken = default)
    {
        var results = await geminiService.GenerateExerciseOptionsAsync(
            targetLanguage: targetLanguage,
            exerciseType: exerciseType,
            title: title,
            instruction: instruction,
            content: content,
            correctAnswer: correctAnswer,
            optionCount: optionCount,
            cancellationToken: cancellationToken);
        return results.Select(o => new GeneratedExerciseOptionDto(o.Text, o.IsCorrect)).ToList();
    }

    public async Task<List<GeneratedExampleSentenceDto>> GenerateExampleSentencesAsync(
        string targetLanguage, string context, int count, string? level,
        List<string>? targetLanguageCodes = null,
        CancellationToken cancellationToken = default)
    {
        var results = await geminiService.GenerateExampleSentencesAsync(targetLanguage, context, count, level, targetLanguageCodes, cancellationToken);
        return results.Select(s => new GeneratedExampleSentenceDto(s.Text, s.Note, s.Translations)).ToList();
    }
}
