using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Pronunciation.Entities;

public sealed class PronunciationCriterionScoreEntity : Entity<Guid>
{
    public Guid AssessmentId { get; private set; }
    public string CriterionCode { get; private set; } = string.Empty;
    public string CriterionName { get; private set; } = string.Empty;
    public string CriterionDescriptionSnapshot { get; private set; } = string.Empty;
    public decimal WeightSnapshot { get; private set; }
    public decimal RawScore { get; private set; }
    public decimal WeightedScore { get; private set; }
    public string Feedback { get; private set; } = string.Empty;

    private PronunciationCriterionScoreEntity()
    {
    }

    public PronunciationCriterionScoreEntity(
        Guid assessmentId,
        string criterionCode,
        string criterionName,
        string criterionDescriptionSnapshot,
        decimal weightSnapshot,
        decimal rawScore,
        decimal weightedScore,
        string feedback,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        AssessmentId = assessmentId;
        CriterionCode = criterionCode?.Trim() ?? string.Empty;
        CriterionName = criterionName?.Trim() ?? string.Empty;
        if (weightSnapshot < 0)
            throw new InvalidOperationException("Trọng số tiêu chí không được âm.");

        WeightSnapshot = weightSnapshot;
        RawScore = Math.Clamp(rawScore, 0m, 100m);
        WeightedScore = weightedScore;
        Feedback = feedback?.Trim() ?? string.Empty;
    }
}
