using EnglishLiveTutor.Domain.Common.Primitives;
using EnglishLiveTutor.Domain.Lessons.Entities;
using EnglishLiveTutor.Domain.Lessons.Enums;

namespace EnglishLiveTutor.Domain.Lessons;

/// <summary>
/// Aggregate root representing an educational lesson comprising multiple exercises.
/// Maintains versioning, publication lifecycle, and aggregate integrity.
/// </summary>
public sealed class LessonEntity : AggregateRoot<Guid>
{
    public static readonly string[] AllowedCefrLevels = ["A1", "A2", "B1", "B2", "C1", "C2"];

    public string Title { get; private set; } = string.Empty;
    public string CefrLevel { get; private set; } = "B1";
    public string Topic { get; private set; } = string.Empty;
    public string LearningObjectivesJson { get; private set; } = "[]";
    public string TargetVocabularyJson { get; private set; } = "[]";
    public string TargetGrammarJson { get; private set; } = "[]";
    public string Content { get; private set; } = string.Empty;
    public int EstimatedMinutes { get; private set; } = 30;
    public LessonStatus Status { get; private set; } = LessonStatus.Draft;
    public int Version { get; private set; } = 1;
    public Guid? ParentLessonId { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public DateTimeOffset? ArchivedAtUtc { get; private set; }

    private readonly List<ExerciseEntity> _exercises = new();
    public IReadOnlyCollection<ExerciseEntity> Exercises => _exercises.AsReadOnly();

    private LessonEntity()
    {
    }

    public LessonEntity(
        string title,
        string cefrLevel,
        string topic,
        string? content = null,
        int estimatedMinutes = 30,
        string? learningObjectivesJson = null,
        string? targetVocabularyJson = null,
        string? targetGrammarJson = null,
        Guid? parentLessonId = null,
        int version = 1,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(topic))
            throw new ArgumentException("Topic is required.", nameof(topic));
        if (estimatedMinutes < 1)
            throw new ArgumentOutOfRangeException(nameof(estimatedMinutes), "EstimatedMinutes must be at least 1.");

        Title = title.Trim();
        Topic = topic.Trim();
        SetCefrLevel(cefrLevel);
        Content = content?.Trim() ?? string.Empty;
        EstimatedMinutes = estimatedMinutes;
        LearningObjectivesJson = string.IsNullOrWhiteSpace(learningObjectivesJson) ? "[]" : learningObjectivesJson.Trim();
        TargetVocabularyJson = string.IsNullOrWhiteSpace(targetVocabularyJson) ? "[]" : targetVocabularyJson.Trim();
        TargetGrammarJson = string.IsNullOrWhiteSpace(targetGrammarJson) ? "[]" : targetGrammarJson.Trim();
        ParentLessonId = parentLessonId;
        Version = version >= 1 ? version : 1;
        Status = LessonStatus.Draft;
    }

    public static LessonEntity Create(
        string title,
        string cefrLevel,
        string topic,
        string? content = null,
        int estimatedMinutes = 30,
        string? learningObjectivesJson = null,
        string? targetVocabularyJson = null,
        string? targetGrammarJson = null,
        Guid? id = null) =>
        new(title, cefrLevel, topic, content, estimatedMinutes, learningObjectivesJson, targetVocabularyJson, targetGrammarJson, null, 1, id);

    public void UpdateDraft(
        string title,
        string cefrLevel,
        string topic,
        string? content,
        int estimatedMinutes,
        string? learningObjectivesJson,
        string? targetVocabularyJson,
        string? targetGrammarJson)
    {
        EnsureDraft();

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(topic))
            throw new ArgumentException("Topic is required.", nameof(topic));
        if (estimatedMinutes < 1)
            throw new ArgumentOutOfRangeException(nameof(estimatedMinutes), "EstimatedMinutes must be at least 1.");

        Title = title.Trim();
        Topic = topic.Trim();
        SetCefrLevel(cefrLevel);
        Content = content?.Trim() ?? string.Empty;
        EstimatedMinutes = estimatedMinutes;
        LearningObjectivesJson = string.IsNullOrWhiteSpace(learningObjectivesJson) ? "[]" : learningObjectivesJson.Trim();
        TargetVocabularyJson = string.IsNullOrWhiteSpace(targetVocabularyJson) ? "[]" : targetVocabularyJson.Trim();
        TargetGrammarJson = string.IsNullOrWhiteSpace(targetGrammarJson) ? "[]" : targetGrammarJson.Trim();
    }

    public ExerciseEntity AddExercise(
        string key,
        ExerciseType type,
        string title,
        string instructions,
        decimal maxPoints,
        GradingMode gradingMode,
        string contentJson,
        string? gradingDefinitionJson = null,
        string? skillTagsJson = null,
        int? order = null,
        Guid? exerciseId = null)
    {
        EnsureDraft();

        string normalizedKey = key.Trim();
        if (_exercises.Any(e => string.Equals(e.Key, normalizedKey, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"An exercise with key '{normalizedKey}' already exists in this lesson.");

        int assignedOrder = order ?? (_exercises.Count > 0 ? _exercises.Max(e => e.Order) + 1 : 1);

        var exercise = new ExerciseEntity(
            Id,
            normalizedKey,
            type,
            title,
            instructions,
            assignedOrder,
            maxPoints,
            gradingMode,
            contentJson,
            gradingDefinitionJson,
            skillTagsJson,
            exerciseId);

        _exercises.Add(exercise);
        NormalizeExerciseOrders();
        return exercise;
    }

    public void UpdateExercise(
        Guid exerciseId,
        string key,
        ExerciseType type,
        string title,
        string instructions,
        decimal maxPoints,
        GradingMode gradingMode,
        string contentJson,
        string? gradingDefinitionJson = null,
        string? skillTagsJson = null)
    {
        EnsureDraft();

        var exercise = _exercises.FirstOrDefault(e => e.Id == exerciseId)
            ?? throw new KeyNotFoundException($"Exercise with ID '{exerciseId}' was not found in lesson.");

        string normalizedKey = key.Trim();
        if (_exercises.Any(e => e.Id != exerciseId && string.Equals(e.Key, normalizedKey, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Another exercise already uses key '{normalizedKey}'.");

        exercise.Update(
            normalizedKey,
            type,
            title,
            instructions,
            maxPoints,
            gradingMode,
            contentJson,
            gradingDefinitionJson,
            skillTagsJson);
    }

    public void RemoveExercise(Guid exerciseId)
    {
        EnsureDraft();

        var exercise = _exercises.FirstOrDefault(e => e.Id == exerciseId)
            ?? throw new KeyNotFoundException($"Exercise with ID '{exerciseId}' was not found in lesson.");

        _exercises.Remove(exercise);
        NormalizeExerciseOrders();
    }

    public void ReorderExercises(IReadOnlyList<Guid> orderedExerciseIds)
    {
        EnsureDraft();

        if (orderedExerciseIds == null || orderedExerciseIds.Count != _exercises.Count)
            throw new ArgumentException("Ordered ID list must contain exactly all exercises in this lesson.", nameof(orderedExerciseIds));

        var exerciseMap = _exercises.ToDictionary(e => e.Id);
        for (int i = 0; i < orderedExerciseIds.Count; i++)
        {
            var id = orderedExerciseIds[i];
            if (!exerciseMap.TryGetValue(id, out var exercise))
                throw new ArgumentException($"Exercise ID '{id}' does not belong to this lesson.", nameof(orderedExerciseIds));

            exercise.SetOrder(i + 1);
        }
    }

    public void Publish(DateTimeOffset publishedAtUtc)
    {
        EnsureDraft();

        if (_exercises.Count == 0)
            throw new InvalidOperationException("Cannot publish a lesson without exercises.");

        // Check sequential order invariant
        var ordered = _exercises.OrderBy(e => e.Order).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            if (ordered[i].Order != i + 1)
                throw new InvalidOperationException($"Exercise orders are not continuous. Expected {i + 1} but found {ordered[i].Order}.");
        }

        // Check unique keys
        var duplicateKeys = _exercises
            .GroupBy(e => e.Key, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateKeys.Count > 0)
            throw new InvalidOperationException($"Duplicate exercise keys found: {string.Join(", ", duplicateKeys)}.");

        Status = LessonStatus.Published;
        PublishedAtUtc = publishedAtUtc;
    }

    public void Archive(DateTimeOffset archivedAtUtc)
    {
        if (Status == LessonStatus.Archived)
            return;

        Status = LessonStatus.Archived;
        ArchivedAtUtc = archivedAtUtc;
    }

    public void RevertToDraft()
    {
        if (Status == LessonStatus.Draft)
            return;

        Status = LessonStatus.Draft;
        PublishedAtUtc = null;
    }

    public LessonEntity CreateDraftRevision(Guid newLessonId, DateTimeOffset createdAtUtc)
    {
        var revision = new LessonEntity(
            Title,
            CefrLevel,
            Topic,
            Content,
            EstimatedMinutes,
            LearningObjectivesJson,
            TargetVocabularyJson,
            TargetGrammarJson,
            parentLessonId: Id,
            version: Version + 1,
            id: newLessonId);

        revision.SetCreationAudit(createdAtUtc);

        foreach (var ex in _exercises.OrderBy(e => e.Order))
        {
            revision.AddExercise(
                ex.Key,
                ex.Type,
                ex.Title,
                ex.Instructions,
                ex.MaxPoints,
                ex.GradingMode,
                ex.ContentJson,
                ex.GradingDefinitionJson,
                ex.SkillTagsJson,
                ex.Order);
        }

        return revision;
    }

    public void SetRowVersion(byte[]? rowVersion)
    {
        RowVersion = rowVersion;
    }

    private void EnsureDraft()
    {
        if (Status != LessonStatus.Draft)
            throw new InvalidOperationException($"Cannot mutate lesson when in '{Status}' status. Must be in 'Draft'.");
    }

    private void SetCefrLevel(string cefrLevel)
    {
        if (string.IsNullOrWhiteSpace(cefrLevel))
            throw new ArgumentException("CEFR level cannot be empty.", nameof(cefrLevel));

        string normalized = cefrLevel.Trim().ToUpperInvariant();
        if (!AllowedCefrLevels.Contains(normalized))
            throw new ArgumentException($"Invalid CEFR level '{cefrLevel}'. Allowed: {string.Join(", ", AllowedCefrLevels)}.", nameof(cefrLevel));

        CefrLevel = normalized;
    }

    private void NormalizeExerciseOrders()
    {
        var ordered = _exercises.OrderBy(e => e.Order).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            ordered[i].SetOrder(i + 1);
        }
    }
}
