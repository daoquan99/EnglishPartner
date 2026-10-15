using EnglishLiveTutor.Domain.Common.Primitives;
using EnglishLiveTutor.Domain.Pronunciation.Entities;

namespace EnglishLiveTutor.Domain.Pronunciation;

public sealed class PronunciationAssessmentEntity : AggregateRoot<Guid>
{
    private List<PronunciationCriterionScoreEntity> _criterionScores = new();

    public string UserId { get; private set; } = "default-learner";
    public Guid VocabularyId { get; private set; }
    public Guid SenseId { get; private set; }
    public string TargetType { get; private set; } = "Word"; // Word | Sentence
    public Guid? TargetId { get; private set; }
    public string ReferenceText { get; private set; } = string.Empty;
    public string Accent { get; private set; } = "US";
    public string ModelCode { get; private set; } = "gemini-3.8-flash";
    public Guid RubricId { get; private set; }
    public int RubricVersion { get; private set; } = 1;
    public Guid AudioRecordingId { get; private set; }
    public string? RecognizedText { get; private set; }
    public decimal? OverallScore { get; private set; }
    public decimal? Confidence { get; private set; }
    public string Status { get; private set; } = "Completed"; // Pending | Processing | Completed | NotAssessable | Failed
    public string? Feedback { get; private set; }
    public string? StrengthsJson { get; private set; }
    public string? ImprovementsJson { get; private set; }
    public string? SuggestedPracticeJson { get; private set; }
    public string? ErrorCode { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public IReadOnlyCollection<PronunciationCriterionScoreEntity> CriterionScores => _criterionScores.AsReadOnly();

    private PronunciationAssessmentEntity()
    {
    }

    public PronunciationAssessmentEntity(
        string userId,
        Guid vocabularyId,
        Guid senseId,
        string targetType,
        Guid? targetId,
        string referenceText,
        string accent,
        string modelCode,
        Guid rubricId,
        int rubricVersion,
        Guid audioRecordingId,
        string? recognizedText,
        decimal? overallScore,
        decimal? confidence,
        string status,
        string? strengthsJson = null,
        string? improvementsJson = null,
        string? suggestedPracticeJson = null,
        DateTimeOffset? completedAt = null,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        UserId = string.IsNullOrWhiteSpace(userId) ? "default-learner" : userId.Trim();
        VocabularyId = vocabularyId;
        SenseId = senseId;
        TargetType = string.IsNullOrWhiteSpace(targetType) ? "Word" : targetType.Trim();
        TargetId = targetId;
        ReferenceText = referenceText?.Trim() ?? string.Empty;
        Accent = string.IsNullOrWhiteSpace(accent) ? "US" : accent.Trim().ToUpperInvariant();
        ModelCode = modelCode?.Trim() ?? "gemini-3.8-flash";
        RubricId = rubricId;
        RubricVersion = rubricVersion <= 0 ? 1 : rubricVersion;
        AudioRecordingId = audioRecordingId;
        RecognizedText = recognizedText;
        OverallScore = overallScore;
        Confidence = confidence;
        Status = string.IsNullOrWhiteSpace(status) ? "Completed" : status.Trim();
        StrengthsJson = strengthsJson;
        ImprovementsJson = improvementsJson;
        SuggestedPracticeJson = suggestedPracticeJson;
        CompletedAt = completedAt;
    }

    public PronunciationCriterionScoreEntity AddCriterionScore(
        string criterionCode,
        string criterionName,
        string criterionDescriptionSnapshot,
        decimal weightSnapshot,
        decimal rawScore,
        decimal weightedScore,
        string feedback,
        Guid? id = null)
    {
        var score = new PronunciationCriterionScoreEntity(
            assessmentId: Id,
            criterionCode: criterionCode,
            criterionName: criterionName,
            criterionDescriptionSnapshot: criterionDescriptionSnapshot,
            weightSnapshot: weightSnapshot,
            rawScore: rawScore,
            weightedScore: weightedScore,
            feedback: feedback,
            id: id);

        _criterionScores.Add(score);
        return score;
    }

    public static decimal CalculateWeightedScore(decimal rawScore, decimal weight)
    {
        return Math.Round(rawScore * weight / 100.00m, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal? CalculateOverallScore(IEnumerable<decimal> weightedScores, bool isAssessable)
    {
        return isAssessable
            ? Math.Round(weightedScores.Sum(), 2, MidpointRounding.AwayFromZero)
            : null;
    }
}
