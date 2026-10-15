using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Pronunciation.Assessments;
using EnglishLiveTutor.Domain.Pronunciation.ScoringRubrics;
using EnglishLiveTutor.Domain.Pronunciation.AudioRecordings;
using EnglishLiveTutor.Domain.Pronunciation.GeneratedAudio;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Ports;

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

public interface IPronunciationSynthesisLock
{
    Task<IDisposable> AcquireLockAsync(string key, CancellationToken ct = default);
}
