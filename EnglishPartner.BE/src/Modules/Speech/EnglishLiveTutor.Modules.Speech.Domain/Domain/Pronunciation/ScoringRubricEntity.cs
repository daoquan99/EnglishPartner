using EnglishLiveTutor.Domain.Common.Primitives;
using EnglishLiveTutor.Domain.Pronunciation.Entities;

namespace EnglishLiveTutor.Domain.Pronunciation;

public sealed class ScoringRubricEntity : AggregateRoot<Guid>
{
    private List<ScoringRubricCriterionEntity> _criteria = new();

    public string Code { get; private set; } = "ENGLISH_PRONUNCIATION_V1";
    public string Name { get; private set; } = "English Pronunciation Rubric v1";
    public string Description { get; private set; } = string.Empty;
    public int Version { get; private set; } = 1;
    public string AssessmentType { get; private set; } = "Pronunciation";
    public string AssessmentScope { get; private set; } = "Pronunciation"; // Pronunciation | LessonExercise
    public int? ExerciseType { get; private set; }
    public string? SubQuestionKind { get; private set; } // ShortAnswer | null
    public string CefrLevel { get; private set; } = "All"; // All | A1 | A2 | B1 | B2 | C1 | C2
    public string? ApplicableExerciseTypes { get; private set; } // e.g. "ReadingComprehension,ListeningComprehension"
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
        string? assessmentScope = null,
        int? exerciseType = null,
        string? subQuestionKind = null,
        string cefrLevel = "All",
        string? applicableExerciseTypes = null,
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
        AssessmentScope = !string.IsNullOrWhiteSpace(assessmentScope) ? assessmentScope.Trim() : AssessmentType;
        ExerciseType = exerciseType;
        SubQuestionKind = subQuestionKind?.Trim();
        CefrLevel = string.IsNullOrWhiteSpace(cefrLevel) ? "All" : cefrLevel.Trim();
        ApplicableExerciseTypes = applicableExerciseTypes?.Trim();
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
        string? descriptorLevel0 = null,
        string? descriptorLevel1 = null,
        string? descriptorLevel2 = null,
        string? descriptorLevel3 = null,
        string? descriptorLevel4 = null,
        Guid? id = null)
    {
        int order = displayOrder ?? (_criteria.Count == 0 ? 1 : _criteria.Max(c => c.DisplayOrder) + 1);
        var criterion = new ScoringRubricCriterionEntity(
            rubricId: Id,
            code: code,
            name: name,
            description: description,
            weight: weight,
            displayOrder: order,
            minimumScore: minimumScore,
            maximumScore: maximumScore,
            isEnabled: isEnabled,
            descriptorLevel0: descriptorLevel0,
            descriptorLevel1: descriptorLevel1,
            descriptorLevel2: descriptorLevel2,
            descriptorLevel3: descriptorLevel3,
            descriptorLevel4: descriptorLevel4,
            id: id);

        _criteria.Add(criterion);
        return criterion;
    }

    public void UpdateRubric(
        string? name,
        string? description,
        IEnumerable<(string Code, string Name, string Description, decimal Weight)> criteria)
    {
        var criteriaList = criteria?.ToList() ?? new();
        if (criteriaList.Count == 0)
            throw new InvalidOperationException("Rubric phải có ít nhất 1 tiêu chí chấm điểm.");

        var candidateList = new List<ScoringRubricCriterionEntity>();
        int order = 1;
        foreach (var c in criteriaList)
        {
            candidateList.Add(new ScoringRubricCriterionEntity(
                rubricId: Id,
                code: c.Code,
                name: c.Name,
                description: c.Description,
                weight: c.Weight,
                displayOrder: order++));
        }

        var duplicateCodes = candidateList
            .GroupBy(c => c.Code.ToUpperInvariant())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicateCodes.Count > 0)
            throw new InvalidOperationException($"Mã tiêu chí bị trùng lặp: {string.Join(", ", duplicateCodes)}.");

        if (candidateList.Any(c => c.Weight < 0))
            throw new InvalidOperationException("Trọng số tiêu chí không được âm.");

        decimal totalWeight = candidateList.Sum(c => c.Weight);
        if (Math.Abs(totalWeight - 100.00m) > 0.01m)
            throw new InvalidOperationException($"Tổng trọng số các tiêu chí phải bằng 100% (hiện tại: {totalWeight}%).");

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
        if (_criteria is null || _criteria.Count == 0)
            throw new InvalidOperationException("Rubric phải có ít nhất 1 tiêu chí chấm điểm.");

        var duplicateCodes = _criteria
            .GroupBy(c => c.Code.ToUpperInvariant())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicateCodes.Count > 0)
            throw new InvalidOperationException($"Mã tiêu chí bị trùng lặp: {string.Join(", ", duplicateCodes)}.");

        if (_criteria.Any(c => c.Weight < 0))
            throw new InvalidOperationException("Trọng số tiêu chí không được âm.");

        decimal totalWeight = _criteria.Sum(c => c.Weight);
        if (Math.Abs(totalWeight - 100.00m) > 0.01m)
            throw new InvalidOperationException($"Tổng trọng số các tiêu chí phải bằng 100% (hiện tại: {totalWeight}%).");
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
