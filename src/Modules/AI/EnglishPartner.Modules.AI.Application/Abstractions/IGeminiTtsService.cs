namespace EnglishPartner.Modules.AI.Application.Abstractions;

/// <summary>
/// Service bridging the Google Cloud TTS v1beta1 API to generate speech 
/// using Gemini Native Audio generative voices with style prompts.
/// </summary>
public interface IGeminiTtsService
{
    /// <summary>
    /// Synthesizes speech using a Gemini Native Audio model.
    /// </summary>
    /// <param name="text">The text to synthesize.</param>
    /// <param name="stylePrompt">Prompt instructing the AI how to read. If null, it falls back to config.</param>
    /// <param name="voiceId">The ID of the voice from LiveVoiceCatalog (e.g., "achernar").</param>
    /// <param name="languageCode">The target language (e.g., "en-US", "vi-VN"). If null, uses Config.DefaultTtsLanguage.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A byte array of MP3 audio data.</returns>
    Task<byte[]> SynthesizeSpeechAsync(
        string text,
        string? stylePrompt = null,
        string voiceId = "zephyr",
        string? languageCode = null,
        CancellationToken cancellationToken = default);
}
