using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Pronunciation;

namespace EnglishLiveTutor.Application.Common.Interfaces;

public sealed record PronunciationAiRawResult(
    string? RecognizedText,
    decimal? Confidence,
    bool IsAssessable,
    Dictionary<string, (decimal RawScore, string Feedback)> CriterionScores,
    List<string> Strengths,
    List<string> Improvements,
    List<string> SuggestedPractice
);

public interface IPronunciationAiAssessor
{
    Task<PronunciationAiRawResult> AssessAudioAsync(
        string apiKey,
        string modelCode,
        string base64Audio,
        string contentType,
        string expectedText,
        string accent,
        ScoringRubricEntity rubric,
        CancellationToken ct = default);
}

public interface ILivePronunciationTtsSynthesizer
{
    Task<SynthesizePronunciationAudioResponseDto> SynthesizeSpeechAsync(
        SynthesizePronunciationAudioRequest request,
        CancellationToken ct = default);
}
