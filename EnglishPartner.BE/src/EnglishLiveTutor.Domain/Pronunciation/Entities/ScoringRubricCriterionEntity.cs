using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Pronunciation.Entities;

public sealed class ScoringRubricCriterionEntity : Entity<Guid>
{
    public Guid RubricId { get; private set; }
    public string Code { get; private set; } = string.Empty; // Accuracy | Clarity | Fluency
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Weight { get; private set; } // 40.00 | 30.00 | 30.00
    public decimal MinimumScore { get; private set; } = 0m;
    public decimal MaximumScore { get; private set; } = 100m;
    public int DisplayOrder { get; private set; }
    public bool IsEnabled { get; private set; } = true;

    private ScoringRubricCriterionEntity()
    {
    }

    public ScoringRubricCriterionEntity(
        Guid rubricId,
        string code,
        string name,
        string description,
        decimal weight,
        int displayOrder = 1,
        decimal minimumScore = 0m,
        decimal maximumScore = 100m,
        bool isEnabled = true,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("Mã tiêu chí không được để trống.");

        RubricId = rubricId;
        Code = code.Trim();
        Name = string.IsNullOrWhiteSpace(name) ? code.Trim() : name.Trim();
        if (weight < 0)
            throw new InvalidOperationException("Trọng số tiêu chí không được âm.");
        if (minimumScore < 0 || maximumScore < 0)
            throw new InvalidOperationException("Điểm không được âm.");
        if (minimumScore > maximumScore)
            throw new InvalidOperationException("Điểm tối thiểu không được lớn hơn điểm tối đa.");

        Weight = weight;
        MinimumScore = minimumScore;
        MaximumScore = maximumScore;
        DisplayOrder = displayOrder <= 0 ? 1 : displayOrder;
        IsEnabled = isEnabled;
    }
}
