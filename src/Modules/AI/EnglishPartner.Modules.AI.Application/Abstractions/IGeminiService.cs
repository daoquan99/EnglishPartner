namespace EnglishPartner.Modules.AI.Application.Abstractions;

/// <summary>
/// Port for communicating with the Gemini LLM.
/// All AI interactions go through this interface.
/// </summary>
public interface IGeminiService
{
    /// <summary>
    /// Evaluate a user's answer and return a quality score (0-5) compatible with SM-2.
    /// </summary>
    Task<EvaluationResult> EvaluateInputAsync(string itemContext, string userInput, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluate a user's spoken audio answer and return a transcription alongside the quality score.
    /// </summary>
    Task<EvaluationResult> EvaluateAudioAsync(string itemContext, byte[] audioData, string mimeType, CancellationToken cancellationToken = default);


    /// <summary>
    /// Send a chat message with history and return the model response.
    /// </summary>
    Task<string> ChatAsync(string systemPrompt, IReadOnlyList<ChatTurn> history, string userMessage, CancellationToken cancellationToken = default);

    // ── Content Generation ──────────────────────────────────────────────────

    /// <summary>Generates vocabulary metadata. When count>1, wordHint is treated as topic context.</summary>
    Task<List<GeminiGeneratedVocabulary>> GenerateVocabularyAsync(string targetLanguage, string wordHint, int count = 1, CancellationToken cancellationToken = default);

    /// <summary>Generates grammar rules with examples translated into targetLanguageCodes.</summary>
    Task<List<GeminiGeneratedGrammar>> GenerateGrammarAsync(string targetLanguage, string title, string? level, int exampleCount, List<string>? targetLanguageCodes = null, int count = 1, CancellationToken cancellationToken = default);

    /// <summary>Generates conversation scripts with line translations into targetLanguageCodes.</summary>
    Task<List<GeminiGeneratedConversation>> GenerateFullConversationAsync(string targetLanguage, string topicName, string? level, string? scenario, int lineCount, List<string>? targetLanguageCodes = null, int count = 1, CancellationToken cancellationToken = default);

    /// <summary>Generates exercises. AI handles text-based types only.</summary>
    Task<List<GeminiGeneratedExercise>> GenerateExerciseAsync(string targetLanguage, string context, int exerciseType, string? level, int count = 1, CancellationToken cancellationToken = default);

    /// <summary>Generates example sentences translated into targetLanguageCodes.</summary>
    Task<List<GeminiGeneratedExampleSentence>> GenerateExampleSentencesAsync(string targetLanguage, string context, int count, string? level, List<string>? targetLanguageCodes = null, CancellationToken cancellationToken = default);
}

public sealed record EvaluationResult(
    int QualityScore,
    bool IsCorrect,
    string SuggestedCorrection,
    string Explanation,
    string Transcription = "");



public sealed record ChatTurn(string Role, string Text);

// ── Content Generation Result Records ──────────────────────────────────────

public sealed record GeminiGeneratedVocabulary(
    string Word,
    string Definition,
    string? Pronunciation,
    string? PhoneticTranscription,
    string? PartOfSpeech,
    string? Level,
    List<string> Synonyms);

public sealed record GeminiGeneratedGrammar(
    string Title,
    string? Explanation,
    string? Formula,
    string? Level,
    List<GeminiGrammarExample> Examples);

public sealed record GeminiGrammarExample(
    string Sentence,
    string? Note,
    Dictionary<string, string> Translations);

public sealed record GeminiGeneratedConversation(
    string Title,
    List<GeminiConversationLine> Lines);

public sealed record GeminiConversationLine(
    string Speaker,
    string Text,
    Dictionary<string, string> Translations);

public sealed record GeminiGeneratedExercise(
    string Title,
    string Instruction,
    string? Content,
    string CorrectAnswer,
    string? Explanation,
    List<GeminiExerciseOption> Options);

public sealed record GeminiExerciseOption(string Text, bool IsCorrect);

public sealed record GeminiGeneratedExampleSentence(
    string Text,
    string? Note,
    Dictionary<string, string> Translations);
