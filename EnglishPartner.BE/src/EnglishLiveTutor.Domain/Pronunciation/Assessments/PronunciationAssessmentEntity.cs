using EnglishLiveTutor.Domain.Common.Primitives;
using EnglishLiveTutor.Domain.Pronunciation.Assessments.Entities;
using EnglishLiveTutor.Domain.Pronunciation.ScoringRubrics;

namespace EnglishLiveTutor.Domain.Pronunciation.Assessments;

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
        if (string.IsNullOrWhiteSpace(referenceText))
            throw new InvalidOperationException("ReferenceText cần chấm phát âm không được để trống.");

        string normalizedStatus = string.IsNullOrWhiteSpace(status) ? "Completed" : status.Trim();
        if (string.Equals(normalizedStatus, "NotAssessable", StringComparison.OrdinalIgnoreCase) && overallScore.HasValue)
        {
            throw new InvalidOperationException("Bản chấm phát âm ở trạng thái NotAssessable không được phép mang OverallScore.");
        }
        if (overallScore.HasValue && (overallScore.Value < 0m || overallScore.Value > 100m))
        {
            throw new InvalidOperationException($"OverallScore ({overallScore.Value}) phải nằm trong khoảng [0, 100].");
        }

        UserId = string.IsNullOrWhiteSpace(userId) ? "default-learner" : userId.Trim();
        VocabularyId = vocabularyId;
        SenseId = senseId;
        TargetType = string.IsNullOrWhiteSpace(targetType) ? "Word" : targetType.Trim();
        TargetId = targetId;
        ReferenceText = referenceText.Trim();
        Accent = string.IsNullOrWhiteSpace(accent) ? "US" : accent.Trim().ToUpperInvariant();
        ModelCode = modelCode?.Trim() ?? "gemini-3.8-flash";
        RubricId = rubricId;
        RubricVersion = rubricVersion <= 0 ? 1 : rubricVersion;
        AudioRecordingId = audioRecordingId;
        RecognizedText = recognizedText;
        OverallScore = overallScore;
        Confidence = confidence.HasValue ? Math.Clamp(confidence.Value, 0m, 1m) : null;
        Status = normalizedStatus;
        StrengthsJson = strengthsJson;
        ImprovementsJson = improvementsJson;
        SuggestedPracticeJson = suggestedPracticeJson;
        CompletedAt = completedAt;
    }

    public static PronunciationAssessmentEntity CreateEvaluated(
        string userId,
        Guid vocabularyId,
        Guid senseId,
        string targetType,
        Guid? targetId,
        string referenceText,
        string accent,
        string modelCode,
        ScoringRubricEntity rubric,
        Guid audioRecordingId,
        bool isAssessable,
        string? recognizedText,
        decimal? confidence,
        IReadOnlyDictionary<string, (decimal RawScore, string Feedback)> rawCriterionScores,
        string? strengthsJson,
        string? improvementsJson,
        string? suggestedPracticeJson,
        DateTimeOffset completedAtUtc,
        Guid? id = null)
    {
        var enabledCriteria = rubric.Criteria
            .Where(c => c.IsEnabled)
            .OrderBy(c => c.DisplayOrder)
            .ToList();

        if (enabledCriteria.Count == 0)
            throw new InvalidOperationException("Rubric chấm phát âm không có tiêu chí nào đang kích hoạt.");

        var stagedScores = new List<(string Code, string Name, string Description, decimal Weight, decimal RawScore, decimal WeightedScore, string Feedback)>(enabledCriteria.Count);

        foreach (var criterion in enabledCriteria)
        {
            decimal rawScore = 0m;
            string feedback = isAssessable
                ? "Phát âm khá rõ ràng."
                : "Không nhận diện được giọng nói rõ ràng.";

            if (rawCriterionScores.TryGetValue(criterion.Code, out var matched))
            {
                rawScore = isAssessable
                    ? Math.Clamp(matched.RawScore, criterion.MinimumScore, criterion.MaximumScore)
                    : 0m;
                if (!string.IsNullOrWhiteSpace(matched.Feedback))
                {
                    feedback = matched.Feedback.Trim();
                }
            }

            decimal weightedScore = CalculateWeightedScore(rawScore, criterion.Weight);
            stagedScores.Add((
                criterion.Code,
                criterion.Name,
                criterion.Description,
                criterion.Weight,
                rawScore,
                weightedScore,
                feedback));
        }

        decimal? overallScore = CalculateOverallScore(stagedScores.Select(s => s.WeightedScore), isAssessable);

        var assessment = new PronunciationAssessmentEntity(
            userId: userId,
            vocabularyId: vocabularyId,
            senseId: senseId,
            targetType: targetType,
            targetId: targetId,
            referenceText: referenceText,
            accent: accent,
            modelCode: modelCode,
            rubricId: rubric.Id,
            rubricVersion: rubric.Version,
            audioRecordingId: audioRecordingId,
            recognizedText: recognizedText,
            overallScore: overallScore,
            confidence: confidence,
            status: isAssessable ? "Completed" : "NotAssessable",
            strengthsJson: strengthsJson,
            improvementsJson: improvementsJson,
            suggestedPracticeJson: suggestedPracticeJson,
            completedAt: completedAtUtc,
            id: id);

        foreach (var item in stagedScores)
        {
            assessment.AddCriterionScore(
                item.Code,
                item.Name,
                item.Description,
                item.Weight,
                item.RawScore,
                item.WeightedScore,
                item.Feedback);
        }

        return assessment;
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
            ? Math.Clamp(Math.Round(weightedScores.Sum(), 2, MidpointRounding.AwayFromZero), 0m, 100m)
            : null;
    }
}
