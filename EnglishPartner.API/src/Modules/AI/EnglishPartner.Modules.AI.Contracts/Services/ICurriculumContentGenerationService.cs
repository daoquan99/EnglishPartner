using EnglishPartner.Modules.AI.Contracts.Services.Dtos;

namespace EnglishPartner.Modules.AI.Contracts.Services;

/// <summary>
/// Cross-module contract for AI-driven curriculum content generation.
/// Implemented in AI.Infrastructure; consumed by Curriculum.Application command handlers.
/// Follows the same pattern as IAiEvaluationService.
/// </summary>
public interface ICurriculumContentGenerationService
{
    /// <summary>
    /// Generate vocabulary metadata (definition, pronunciation, synonyms, etc.) for a word.
    /// </summary>
    /// <param name="targetLanguage">The language to generate content in (e.g. en, ja, ko).</param>
    /// <param name="wordHint">Specific word or topic context. AI picks N related words when count > 1.</param>
    /// <param name="count">Number of vocabulary entries to generate.</param>
    Task<List<GeneratedVocabularyDto>> GenerateVocabularyAsync(
        string targetLanguage,
        string wordHint,
        int count = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate grammar rules with explanation, formula, and examples in multiple languages.
    /// </summary>
    /// <param name="targetLanguage">The language to generate content in (e.g. en, ja, ko).</param>
    /// <param name="targetLanguageCodes">Additional languages for example translations e.g. ["vi","ja"].</param>
    /// <param name="count">Number of grammar rules to generate.</param>
    Task<List<GeneratedGrammarDto>> GenerateGrammarAsync(
        string targetLanguage,
        string title,
        string? level,
        int exampleCount,
        List<string>? targetLanguageCodes = null,
        int count = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate conversation scripts (title + dialogue lines) with translations.
    /// </summary>
    /// <param name="targetLanguage">The language to generate content in (e.g. en, ja, ko).</param>
    /// <param name="targetLanguageCodes">Languages for line translations e.g. ["vi","ko"].</param>
    /// <param name="count">Number of conversation scripts to generate.</param>
    Task<List<GeneratedConversationContentDto>> GenerateConversationAsync(
        string targetLanguage,
        string topicName,
        string? level,
        string? scenario,
        int lineCount,
        List<string>? targetLanguageCodes = null,
        int count = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate exercises. Supported types: MultipleChoice, FillInTheBlank, TrueFalse,
    /// Translation, SentenceCompletion, WordForm, VerbTense, GapFilling, Paraphrasing,
    /// ReorderingWords, DialogueCompletion, ShortAnswerQuestions.
    /// </summary>
    /// <param name="targetLanguage">The language to generate content in (e.g. en, ja, ko).</param>
    /// <param name="count">Number of exercises to generate.</param>
    Task<List<GeneratedExerciseDto>> GenerateExerciseAsync(
        string targetLanguage,
        string context,
        int exerciseType,
        string? level,
        int count = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate answer options for an existing exercise.
    /// </summary>
    Task<List<GeneratedExerciseOptionDto>> GenerateExerciseOptionsAsync(
        string targetLanguage,
        int exerciseType,
        string title,
        string instruction,
        string? content,
        string correctAnswer,
        int optionCount = 4,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate N example sentences illustrating a vocabulary word or grammar rule.
    /// </summary>
    /// <param name="targetLanguage">The language to generate content in (e.g. en, ja, ko).</param>
    /// <param name="targetLanguageCodes">Languages for sentence translations e.g. ["vi","ja"].</param>
    Task<List<GeneratedExampleSentenceDto>> GenerateExampleSentencesAsync(
        string targetLanguage,
        string context,
        int count,
        string? level,
        List<string>? targetLanguageCodes = null,
        CancellationToken cancellationToken = default);
}
