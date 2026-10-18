using EnglishLiveTutor.Domain.Common.Primitives;
using EnglishLiveTutor.Domain.Lessons.Enums;

namespace EnglishLiveTutor.Domain.Lessons.Entities;

/// <summary>
/// Represents an individual exercise within a lesson.
/// Contains the authoring content and grading configuration.
/// </summary>
public sealed class ExerciseEntity : Entity<Guid>
{
    public Guid LessonId { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public ExerciseType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Instructions { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public decimal MaxPoints { get; private set; }
    public GradingMode GradingMode { get; private set; }
    public string? SkillTagsJson { get; private set; }
    public string ContentJson { get; private set; } = "{}";
    public string? GradingDefinitionJson { get; private set; }

    private ExerciseEntity()
    {
    }

    public ExerciseEntity(
        Guid lessonId,
        string key,
        ExerciseType type,
        string title,
        string instructions,
        int order,
        decimal maxPoints,
        GradingMode gradingMode,
        string contentJson,
        string? gradingDefinitionJson = null,
        string? skillTagsJson = null,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (lessonId == Guid.Empty)
            throw new ArgumentException("Lesson ID cannot be empty.", nameof(lessonId));
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Exercise key is required.", nameof(key));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Exercise title is required.", nameof(title));
        if (order < 1)
            throw new ArgumentOutOfRangeException(nameof(order), "Exercise order must be at least 1.");
        if (maxPoints < 0)
            throw new ArgumentOutOfRangeException(nameof(maxPoints), "MaxPoints cannot be negative.");
        if (string.IsNullOrWhiteSpace(contentJson))
            throw new ArgumentException("ContentJson is required.", nameof(contentJson));

        LessonId = lessonId;
        Key = key.Trim();
        Type = type;
        Title = title.Trim();
        Instructions = instructions?.Trim() ?? string.Empty;
        Order = order;
        MaxPoints = maxPoints;
        GradingMode = gradingMode;
        ContentJson = contentJson.Trim();
        GradingDefinitionJson = string.IsNullOrWhiteSpace(gradingDefinitionJson) ? null : gradingDefinitionJson.Trim();
        SkillTagsJson = string.IsNullOrWhiteSpace(skillTagsJson) ? null : skillTagsJson.Trim();
    }

    public void Update(
        string key,
        ExerciseType type,
        string title,
        string instructions,
        decimal maxPoints,
        GradingMode gradingMode,
        string contentJson,
        string? gradingDefinitionJson,
        string? skillTagsJson)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Exercise key is required.", nameof(key));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Exercise title is required.", nameof(title));
        if (maxPoints < 0)
            throw new ArgumentOutOfRangeException(nameof(maxPoints), "MaxPoints cannot be negative.");
        if (string.IsNullOrWhiteSpace(contentJson))
            throw new ArgumentException("ContentJson is required.", nameof(contentJson));

        Key = key.Trim();
        Type = type;
        Title = title.Trim();
        Instructions = instructions?.Trim() ?? string.Empty;
        MaxPoints = maxPoints;
        GradingMode = gradingMode;
        ContentJson = contentJson.Trim();
        GradingDefinitionJson = string.IsNullOrWhiteSpace(gradingDefinitionJson) ? null : gradingDefinitionJson.Trim();
        SkillTagsJson = string.IsNullOrWhiteSpace(skillTagsJson) ? null : skillTagsJson.Trim();
    }

    public void SetOrder(int order)
    {
        if (order < 1)
            throw new ArgumentOutOfRangeException(nameof(order), "Exercise order must be at least 1.");
        Order = order;
    }

    public void SetContent(string contentJson)
    {
        if (string.IsNullOrWhiteSpace(contentJson))
            throw new ArgumentException("ContentJson is required.", nameof(contentJson));
        ContentJson = contentJson.Trim();
    }

    public void SetGradingDefinition(string? gradingDefinitionJson)
    {
        GradingDefinitionJson = string.IsNullOrWhiteSpace(gradingDefinitionJson) ? null : gradingDefinitionJson.Trim();
    }
}
