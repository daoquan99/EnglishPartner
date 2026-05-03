namespace EnglishPartner.Modules.AI.Contracts.Services;

/// <summary>
/// Module Interface — allows other modules (e.g. Learning) to request AI evaluation
/// of a user's answer without coupling to the AI Application or Infrastructure layers.
/// Per architecture rules: cross-module sync calls use Module Interfaces in Contracts.
/// </summary>
public interface IAiEvaluationService
{
    /// <summary>
    /// Evaluates a user's text answer against a curriculum item.
    /// Used for open-ended or complex text questions.
    /// </summary>
    /// <param name="question">The question or prompt.</param>
    /// <param name="correctAnswer">The expected correct answer (if any).</param>
    /// <param name="userAnswer">The user's actual input.</param>
    /// <returns>Score from 0 to 100 and feedback.</returns>
    Task<AiTextEvaluationResult?> EvaluateTextAnswerAsync(
        string question,
        string? correctAnswer,
        string userAnswer,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates a user's spoken audio against a target text.
    /// </summary>
    /// <param name="targetText">The exact text the user was supposed to read.</param>
    /// <param name="audioStream">The audio file stream.</param>
    /// <param name="targetLanguage">The language code (e.g. "en-US").</param>
    /// <param name="mimeType">The MIME type of the audio file (e.g. "audio/webm", "audio/mpeg").</param>
    /// <returns>Score from 0 to 100 and pronunciation feedback.</returns>
    Task<AiAudioEvaluationResult?> EvaluatePronunciationAsync(
        string targetText,
        Stream audioStream,
        string targetLanguage,
        string mimeType = "audio/webm",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates speech audio from text using AI Text-To-Speech.
    /// </summary>
    /// <param name="text">The text to speak.</param>
    /// <param name="targetLanguage">Language code.</param>
    /// <param name="voiceOptions">Optional voice characteristics (e.g., "male", "female").</param>
    /// <returns>Audio stream (e.g. mp3 or wav data).</returns>
    Task<Stream?> GenerateTtsAsync(
        string text,
        string targetLanguage,
        string? voiceOptions = null,
        CancellationToken cancellationToken = default);
}

public sealed record AiTextEvaluationResult(
    int Score, // 0-100
    bool IsCorrect,
    string? SuggestedCorrection,
    string? Explanation);

public sealed record AiAudioEvaluationResult(
    int Score, // 0-100
    string Transcription,
    string? PronunciationFeedback);
