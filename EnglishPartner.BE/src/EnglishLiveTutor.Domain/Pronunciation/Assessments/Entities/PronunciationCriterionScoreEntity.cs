using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Pronunciation.Assessments.Entities;

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
        if (string.IsNullOrWhiteSpace(criterionCode))
            throw new InvalidOperationException("Mã tiêu chí chấm điểm không được để trống.");
        if (weightSnapshot <= 0m || weightSnapshot > 100m)
            throw new InvalidOperationException($"Trọng số tiêu chí '{criterionCode}' phải nằm trong khoảng (0, 100].");
        if (rawScore < 0m || rawScore > 100m)
            throw new InvalidOperationException($"Điểm thành phần '{criterionCode}' ({rawScore}) phải nằm trong khoảng [0, 100].");
        if (weightedScore < 0m || weightedScore > 100m)
            throw new InvalidOperationException($"Điểm trọng số '{criterionCode}' ({weightedScore}) phải nằm trong khoảng [0, 100].");

        AssessmentId = assessmentId;
        CriterionCode = criterionCode.Trim();
        CriterionName = string.IsNullOrWhiteSpace(criterionName) ? CriterionCode : criterionName.Trim();
        CriterionDescriptionSnapshot = criterionDescriptionSnapshot?.Trim() ?? string.Empty;
        WeightSnapshot = weightSnapshot;
        RawScore = rawScore;
        WeightedScore = weightedScore;
        Feedback = feedback?.Trim() ?? string.Empty;
    }
}
