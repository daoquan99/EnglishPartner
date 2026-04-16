namespace EnglishPartner.Modules.AI.Application.Abstractions;

/// <summary>
/// Port for Google Cloud Text-to-Speech and Speech-to-Text APIs.
/// </summary>
public interface ISpeechService
{
    /// <summary>
    /// Synthesizes speech from text and returns raw MP3 audio bytes.
    /// </summary>
    /// <param name="text">Text to convert to speech.</param>
    /// <param name="voice">Voice name override (e.g. "en-US-Neural2-F"). Null = config default.</param>
    /// <param name="speakingRate">Speed multiplier 0.25–4.0. Null = config default (1.0).</param>
    Task<byte[]> SynthesizeSpeechAsync(
        string text,
        string? voice = null,
        double? speakingRate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transcribes audio to text with optional language auto-detection.
    /// Pass multiple language codes for Google to detect among them (like Google Translate auto-detect).
    /// Pass null/empty to use the server default language ("en-US").
    /// </summary>
    /// <param name="audioData">Raw audio bytes.</param>
    /// <param name="mimeType">MIME type of the audio (e.g. "audio/webm", "audio/wav").</param>
    /// <param name="languageCodes">
    /// List of BCP-47 language codes.
    /// - 1 code  → use that language directly
    /// - 2+ codes → auto-detect among candidates (e.g. ["en-US", "vi-VN"])
    /// - null/empty → use server default ("en-US")
    /// </param>
    Task<SttResult> RecognizeSpeechAsync(
        byte[] audioData,
        string mimeType,
        IReadOnlyList<string>? languageCodes = null,
        CancellationToken cancellationToken = default);
}

/// <summary>Result from Speech-to-Text recognition.</summary>
public sealed record SttResult(
    string Transcript,
    double Confidence,
    /// <summary>BCP-47 code of the detected language (e.g. "en-US"). Null if only one language was specified.</summary>
    string? DetectedLanguage = null);
