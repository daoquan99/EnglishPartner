using System.Text.Json;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Lessons.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Lessons.Queries.GetLessonById;

public sealed record GetLessonByIdQuery(Guid LessonId, bool IsLearner = false) : IQuery<object?>;

public sealed class GetLessonByIdQueryHandler : IQueryHandler<GetLessonByIdQuery, object?>
{
    private readonly IAppDbContext _db;
    private readonly ILessonSanitizer _sanitizer;

    public GetLessonByIdQueryHandler(IAppDbContext db, ILessonSanitizer sanitizer)
    {
        _db = db;
        _sanitizer = sanitizer;
    }

    public async Task<object?> HandleAsync(GetLessonByIdQuery query, CancellationToken ct = default)
    {
        var lesson = await _db.Lessons
            .AsNoTracking()
            .Include(l => l.Exercises)
            .FirstOrDefaultAsync(l => l.Id == query.LessonId, ct);

        if (lesson == null)
            return null;

        var objectives = DeserializeStringList(lesson.LearningObjectivesJson);
        var vocab = DeserializeStringList(lesson.TargetVocabularyJson);
        var grammar = DeserializeStringList(lesson.TargetGrammarJson);

        if (query.IsLearner)
        {
            if (lesson.Status != LessonStatus.Published)
                return null; // Learners cannot view Draft or Archived lessons

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
