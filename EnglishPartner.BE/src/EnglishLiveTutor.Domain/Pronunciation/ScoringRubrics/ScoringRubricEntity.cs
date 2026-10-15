using EnglishLiveTutor.Domain.Common.Primitives;
using EnglishLiveTutor.Domain.Pronunciation.ScoringRubrics.Entities;

namespace EnglishLiveTutor.Domain.Pronunciation.ScoringRubrics;

public sealed class ScoringRubricEntity : AggregateRoot<Guid>
{
    private List<ScoringRubricCriterionEntity> _criteria = new();

    public string Code { get; private set; } = "ENGLISH_PRONUNCIATION_V1";
    public string Name { get; private set; } = "English Pronunciation Rubric v1";
    public string Description { get; private set; } = string.Empty;
    public int Version { get; private set; } = 1;
    public string AssessmentType { get; private set; } = "Pronunciation";
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<ScoringRubricCriterionEntity> Criteria => _criteria.AsReadOnly();

    private ScoringRubricEntity()
    {
    }

    public ScoringRubricEntity(
        string code,
        string name,
        string description = "",
        int version = 1,
        string assessmentType = "Pronunciation",
        bool isActive = true,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("Mã Rubric không được để trống.");
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Tên Rubric không được để trống.");

        Code = code.Trim();
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Version = version <= 0 ? 1 : version;
        AssessmentType = string.IsNullOrWhiteSpace(assessmentType) ? "Pronunciation" : assessmentType.Trim();
        IsActive = isActive;
    }

    public ScoringRubricCriterionEntity AddCriterion(
        string code,
        string name,
        string description,
        decimal weight,
        int? displayOrder = null,
        decimal minimumScore = 0m,
        decimal maximumScore = 100m,
        bool isEnabled = true,
        Guid? id = null)
    {
        string normalizedCode = string.IsNullOrWhiteSpace(code)
            ? throw new InvalidOperationException("Mã tiêu chí không được để trống.")
            : code.Trim();

        if (_criteria.Any(c => string.Equals(c.Code, normalizedCode, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Tiêu chí chấm điểm bị trùng mã '{normalizedCode}'.");
        }

        int order = displayOrder ?? (_criteria.Count == 0 ? 1 : _criteria.Max(c => c.DisplayOrder) + 1);
        var criterion = new ScoringRubricCriterionEntity(
            rubricId: Id,
            code: normalizedCode,
            name: name,
            description: description,
            weight: weight,
            displayOrder: order,
            minimumScore: minimumScore,
            maximumScore: maximumScore,
            isEnabled: isEnabled,
            id: id);

        _criteria.Add(criterion);
        return criterion;
    }

    public void UpdateRubric(
        string? name,
        string? description,
        IEnumerable<(string Code, string Name, string Description, decimal Weight)> criteria)
    {
        if (criteria is null)
            throw new InvalidOperationException("Rubric phải có ít nhất 1 tiêu chí chấm điểm.");

        var candidateList = new List<ScoringRubricCriterionEntity>();
        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int order = 1;

        foreach (var c in criteria)
        {
            string normalizedCode = string.IsNullOrWhiteSpace(c.Code)
                ? throw new InvalidOperationException("Mã tiêu chí không được để trống.")
                : c.Code.Trim();

            if (!seenCodes.Add(normalizedCode))
            {
                throw new InvalidOperationException($"Tiêu chí chấm điểm bị trùng mã '{normalizedCode}'.");
            }

            candidateList.Add(new ScoringRubricCriterionEntity(
                rubricId: Id,
                code: normalizedCode,
                name: c.Name,
                description: c.Description,
                weight: c.Weight,
                displayOrder: order++));
        }

        ValidateCandidateCriteria(candidateList);

        if (!string.IsNullOrWhiteSpace(name))
        {
            Name = name.Trim();
        }

        if (description is not null)
        {
            Description = description.Trim();
        }

        _criteria.Clear();
        _criteria.AddRange(candidateList);
    }

    public void ValidateWeights()
    {
        ValidateCandidateCriteria(_criteria);
    }

    private static void ValidateCandidateCriteria(IReadOnlyCollection<ScoringRubricCriterionEntity>? criteria)
    {
        if (criteria is null || criteria.Count == 0)
            throw new InvalidOperationException("Rubric phải có ít nhất 1 tiêu chí chấm điểm.");

        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var criterion in criteria)
        {
            if (!seenCodes.Add(criterion.Code))
                throw new InvalidOperationException($"Tiêu chí chấm điểm bị trùng mã '{criterion.Code}'.");
            if (criterion.Weight <= 0m || criterion.Weight > 100m)
                throw new InvalidOperationException($"Trọng số của tiêu chí '{criterion.Code}' phải lớn hơn 0 và không vượt quá 100.");
            if (criterion.MinimumScore < 0m || criterion.MaximumScore <= criterion.MinimumScore || criterion.MaximumScore > 100m)
                throw new InvalidOperationException($"Khoảng điểm của tiêu chí '{criterion.Code}' không hợp lệ.");
        }

        var enabled = criteria.Where(c => c.IsEnabled).ToList();
        if (enabled.Count == 0)
            throw new InvalidOperationException("Rubric phải có ít nhất 1 tiêu chí đang được kích hoạt (IsEnabled = true).");

        decimal totalWeight = enabled.Sum(c => c.Weight);
        if (Math.Abs(totalWeight - 100.00m) > 0.01m)
            throw new InvalidOperationException($"Tổng trọng số các tiêu chí phải bằng 100% (hiện tại: {totalWeight}%).");
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
