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

    public string DescriptorLevel0 { get; private set; } = string.Empty;
    public string DescriptorLevel1 { get; private set; } = string.Empty;
    public string DescriptorLevel2 { get; private set; } = string.Empty;
    public string DescriptorLevel3 { get; private set; } = string.Empty;
    public string DescriptorLevel4 { get; private set; } = string.Empty;

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
        string? descriptorLevel0 = null,
        string? descriptorLevel1 = null,
        string? descriptorLevel2 = null,
        string? descriptorLevel3 = null,
        string? descriptorLevel4 = null,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("Mã tiêu chí không được để trống.");

        RubricId = rubricId;
        Code = code.Trim();
        Name = string.IsNullOrWhiteSpace(name) ? code.Trim() : name.Trim();
        Description = description?.Trim() ?? string.Empty;
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

        DescriptorLevel0 = descriptorLevel0?.Trim() ?? string.Empty;
        DescriptorLevel1 = descriptorLevel1?.Trim() ?? string.Empty;
        DescriptorLevel2 = descriptorLevel2?.Trim() ?? string.Empty;
        DescriptorLevel3 = descriptorLevel3?.Trim() ?? string.Empty;
        DescriptorLevel4 = descriptorLevel4?.Trim() ?? string.Empty;
    }

    public void SetDescriptors(string? l0, string? l1, string? l2, string? l3, string? l4)
    {
        DescriptorLevel0 = l0?.Trim() ?? string.Empty;
        DescriptorLevel1 = l1?.Trim() ?? string.Empty;
        DescriptorLevel2 = l2?.Trim() ?? string.Empty;
        DescriptorLevel3 = l3?.Trim() ?? string.Empty;
        DescriptorLevel4 = l4?.Trim() ?? string.Empty;
    }
}
