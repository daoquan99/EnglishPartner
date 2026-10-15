using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Prompts;

/// <summary>
/// Aggregate Root representing a configurable Teacher Speech Speed option stored in the Database.
/// </summary>
public sealed class SpeechSpeedEntity : AggregateRoot<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public double SpeedValue { get; private set; } = 1.0;
    public string Label { get; private set; } = string.Empty;
    public string DescriptionVi { get; private set; } = string.Empty;
    public string RecommendedCefrRange { get; private set; } = "B1 - B2";
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int DisplayOrder { get; private set; } = 1;

    private SpeechSpeedEntity()
    {
    }

    public SpeechSpeedEntity(
        string code,
        double speedValue,
        string label,
        string descriptionVi,
        string recommendedCefrRange = "B1 - B2",
        bool isDefault = false,
        bool isActive = true,
        int displayOrder = 1,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        Code = string.IsNullOrWhiteSpace(code) ? $"{speedValue:0.00}x" : code.Trim();
        SpeedValue = Math.Clamp(speedValue, 0.5, 2.0);
        Label = string.IsNullOrWhiteSpace(label) ? $"{SpeedValue:0.00}x" : label.Trim();
        DescriptionVi = descriptionVi?.Trim() ?? string.Empty;
        RecommendedCefrRange = string.IsNullOrWhiteSpace(recommendedCefrRange) ? "A1 - C2" : recommendedCefrRange.Trim();
        IsDefault = isDefault;
        IsActive = isActive;
        DisplayOrder = displayOrder;
    }

    public void UpdateDetails(
        string code,
        double speedValue,
        string label,
        string descriptionVi,
        string recommendedCefrRange,
        bool isDefault,
        bool isActive,
        int displayOrder)
    {
        if (!string.IsNullOrWhiteSpace(code))
            Code = code.Trim();
        SpeedValue = Math.Clamp(speedValue, 0.5, 2.0);
        if (!string.IsNullOrWhiteSpace(label))
            Label = label.Trim();
        DescriptionVi = descriptionVi?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(recommendedCefrRange))
            RecommendedCefrRange = recommendedCefrRange.Trim();
        IsDefault = isDefault;
        IsActive = isActive;
        DisplayOrder = displayOrder;
    }

    public override void SoftDelete(DateTimeOffset deletedAtUtc, string? deletedBy = null)
    {
        base.SoftDelete(deletedAtUtc, deletedBy);
        IsActive = false;
    }
}
