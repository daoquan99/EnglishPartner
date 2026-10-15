using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Domain.Pronunciation.ScoringRubrics;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Ports;

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
