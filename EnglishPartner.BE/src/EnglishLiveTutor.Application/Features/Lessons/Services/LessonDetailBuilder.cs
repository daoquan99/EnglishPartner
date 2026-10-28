using System.Text.Json;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Lessons.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Lessons.Services;

public interface ILessonDetailBuilder
{
    Task<object?> BuildAsync(Guid lessonId, bool isLearner, CancellationToken ct = default);
    Task<LessonDetailAuthorDto?> BuildAuthorDetailAsync(Guid lessonId, CancellationToken ct = default);
    Task<LessonDetailLearnerDto?> BuildLearnerDetailAsync(Guid lessonId, CancellationToken ct = default);
}

public sealed class LessonDetailBuilder : ILessonDetailBuilder
{
    private readonly IAppDbContext _db;
    private readonly ILessonSanitizer _sanitizer;

    public LessonDetailBuilder(IAppDbContext db, ILessonSanitizer sanitizer)
    {
        _db = db;
        _sanitizer = sanitizer;
    }

    public async Task<object?> BuildAsync(Guid lessonId, bool isLearner, CancellationToken ct = default)
    {
        if (isLearner)
            return await BuildLearnerDetailAsync(lessonId, ct);
        return await BuildAuthorDetailAsync(lessonId, ct);
    }

    public async Task<LessonDetailLearnerDto?> BuildLearnerDetailAsync(Guid lessonId, CancellationToken ct = default)
    {
        var lesson = await _db.Lessons
            .AsNoTracking()
            .Include(l => l.Exercises)
            .FirstOrDefaultAsync(l => l.Id == lessonId, ct);

        if (lesson == null || lesson.Status != LessonStatus.Published)
            return null; // Learners cannot view Draft or Archived lessons

        var objectives = DeserializeStringList(lesson.LearningObjectivesJson);
        var vocab = DeserializeStringList(lesson.TargetVocabularyJson);
        var grammar = DeserializeStringList(lesson.TargetGrammarJson);

        var learnerExercises = lesson.Exercises
            .OrderBy(e => e.Order)
            .Select(e =>
            {
                string safeContent = _sanitizer.SanitizeContentForLearner(e.Type, e.ContentJson);
                return new ExerciseLearnerDto(
                    Id: e.Id,
                    Key: e.Key,
                    Type: e.Type.ToString(),
                    Title: e.Title,
                    Instructions: e.Instructions,
                    Order: e.Order,
                    MaxPoints: e.MaxPoints,
                    GradingMode: e.GradingMode.ToString(),
                    Content: ParseJson(safeContent));
            }).ToList();

        return new LessonDetailLearnerDto(
            Id: lesson.Id,
            Title: lesson.Title,
            CefrLevel: lesson.CefrLevel,
            Topic: lesson.Topic,
            LearningObjectives: objectives,
            TargetVocabulary: vocab,
            TargetGrammar: grammar,
            Content: lesson.Content,
            EstimatedMinutes: lesson.EstimatedMinutes,
            Version: lesson.Version,
            Exercises: learnerExercises);
    }

    public async Task<LessonDetailAuthorDto?> BuildAuthorDetailAsync(Guid lessonId, CancellationToken ct = default)
    {
        var lesson = await _db.Lessons
            .AsNoTracking()
            .Include(l => l.Exercises)
            .FirstOrDefaultAsync(l => l.Id == lessonId, ct);

        if (lesson == null)
            return null;

        var objectives = DeserializeStringList(lesson.LearningObjectivesJson);
        var vocab = DeserializeStringList(lesson.TargetVocabularyJson);
        var grammar = DeserializeStringList(lesson.TargetGrammarJson);

        var authorExercises = lesson.Exercises
            .OrderBy(e => e.Order)
            .Select(e => new ExerciseAuthorDto(
                Id: e.Id,
                Key: e.Key,
                Type: e.Type.ToString(),
                Title: e.Title,
                Instructions: e.Instructions,
                Order: e.Order,
                MaxPoints: e.MaxPoints,
                GradingMode: e.GradingMode.ToString(),
                SkillTags: DeserializeStringList(e.SkillTagsJson),
                Content: ParseJson(e.ContentJson),
                Grading: ParseJson(e.GradingDefinitionJson)
            )).ToList();

        return new LessonDetailAuthorDto(
            Id: lesson.Id,
            Title: lesson.Title,
            CefrLevel: lesson.CefrLevel,
            Topic: lesson.Topic,
            LearningObjectives: objectives,
            TargetVocabulary: vocab,
            TargetGrammar: grammar,
            Content: lesson.Content,
            EstimatedMinutes: lesson.EstimatedMinutes,
            Status: lesson.Status.ToString(),
            Version: lesson.Version,
            RowVersion: lesson.RowVersion != null ? Convert.ToBase64String(lesson.RowVersion) : null,
            PublishedAtUtc: lesson.PublishedAtUtc,
            Exercises: authorExercises);
    }

    private static List<string> DeserializeStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static JsonNode? ParseJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonNode.Parse(json);
        }
        catch
        {
            return null;
        }
    }
}
