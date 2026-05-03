using EnglishPartner.Modules.AI.Application.Abstractions;
using EnglishPartner.Modules.AI.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace EnglishPartner.Modules.AI.Infrastructure.Services;

/// <summary>
/// Implements IAiEvaluationService using Gemini LLM.
/// </summary>
internal sealed class AiEvaluationService(
    IGeminiService geminiService,
    ISpeechService speechService,
    ILogger<AiEvaluationService> logger) : IAiEvaluationService
{
    public async Task<AiTextEvaluationResult?> EvaluateTextAnswerAsync(
        string question,
        string? correctAnswer,
        string userAnswer,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var contextString = $"Question: {question}";
            if (!string.IsNullOrWhiteSpace(correctAnswer))
            {
                contextString += $"\nExpected Answer: {correctAnswer}";
            }

            var result = await geminiService.EvaluateInputAsync(contextString, userAnswer, cancellationToken);

            return new AiTextEvaluationResult(
                result.Score,
                result.IsCorrect,
                result.SuggestedCorrection,
                result.Explanation);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI evaluation failed for text answer.");
            return null;
        }
    }

    public async Task<AiAudioEvaluationResult?> EvaluatePronunciationAsync(
        string targetText,
        Stream audioStream,
        string targetLanguage,
        string mimeType = "audio/webm",
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var ms = new MemoryStream();
            await audioStream.CopyToAsync(ms, cancellationToken);
            var audioBytes = ms.ToArray();

            var contextString = $"Target Text to Read: {targetText}\nTarget Language: {targetLanguage}";

            var result = await geminiService.EvaluateAudioAsync(contextString, audioBytes, mimeType, cancellationToken);

            return new AiAudioEvaluationResult(
                result.Score,
                result.Transcription,
                result.Explanation); // We use Explanation from Gemini as the PronunciationFeedback
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI audio evaluation failed.");
            return null;
        }
    }

    public async Task<Stream?> GenerateTtsAsync(
        string text,
        string targetLanguage,
        string? voiceOptions = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var audioBytes = await speechService.SynthesizeSpeechAsync(
                text,
                voiceOptions,
                speakingRate: null,
                cancellationToken);

            return new MemoryStream(audioBytes);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "TTS generation failed for text: {TextSnippet}", text[..Math.Min(50, text.Length)]);
            return null;
        }
    }
}
