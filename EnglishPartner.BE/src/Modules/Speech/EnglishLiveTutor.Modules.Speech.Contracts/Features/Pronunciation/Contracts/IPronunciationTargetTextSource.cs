namespace EnglishLiveTutor.Application.Features.Pronunciation.Contracts;

public sealed record PronunciationAssessmentVocabularyInfo(
    string Word,
    IReadOnlyCollection<PronunciationTargetExampleInfo> Examples);

public sealed record PronunciationTargetWordInfo(
    Guid Id,
    string Word);

public sealed record PronunciationTargetExampleInfo(
    Guid Id,
    Guid SenseId,
    string SentenceEn);

public sealed record PronunciationTargetSenseInfo(
    Guid Id,
    Guid VocabularyId);

public interface IPronunciationTargetTextSource
{
    Task<PronunciationAssessmentVocabularyInfo?> GetAssessmentVocabularyAsync(
        Guid vocabularyId,
        CancellationToken ct = default);

    Task<PronunciationTargetWordInfo?> FindWordAsync(
        Guid vocabularyId,
        CancellationToken ct = default);

    Task<PronunciationTargetExampleInfo?> FindExampleAsync(
        Guid exampleId,
        CancellationToken ct = default);

    Task<PronunciationTargetSenseInfo?> FindSenseAsync(
        Guid senseId,
        CancellationToken ct = default);
}
