using System.Text.Json;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Lessons.Queries.GetLessonById;
using EnglishLiveTutor.Domain.Lessons.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Lessons.Commands.ExerciseOperations;

public sealed record AddExerciseToDraftCommand(Guid LessonId, AddExerciseRequest Request) : ICommand<LessonDetailAuthorDto>;

public sealed record UpdateDraftExerciseCommand(Guid LessonId, Guid ExerciseId, UpdateExerciseRequest Request) : ICommand<LessonDetailAuthorDto>;

public sealed record RemoveDraftExerciseCommand(Guid LessonId, Guid ExerciseId) : ICommand<LessonDetailAuthorDto>;

public sealed record ReorderDraftExercisesCommand(Guid LessonId, ReorderExercisesRequest Request) : ICommand<LessonDetailAuthorDto>;

public sealed class ExerciseCommandHandlers :
    ICommandHandler<AddExerciseToDraftCommand, LessonDetailAuthorDto>,
    ICommandHandler<UpdateDraftExerciseCommand, LessonDetailAuthorDto>,
    ICommandHandler<RemoveDraftExerciseCommand, LessonDetailAuthorDto>,
    ICommandHandler<ReorderDraftExercisesCommand, LessonDetailAuthorDto>
{
    private readonly IAppDbContext _db;
    private readonly IQueryHandler<GetLessonByIdQuery, object?> _getLessonByIdHandler;

    public ExerciseCommandHandlers(
        IAppDbContext db,
        IQueryHandler<GetLessonByIdQuery, object?> getLessonByIdHandler)
    {
        _db = db;
        _getLessonByIdHandler = getLessonByIdHandler;
    }

    public async Task<LessonDetailAuthorDto> HandleAsync(AddExerciseToDraftCommand command, CancellationToken ct = default)
    {
        var lesson = await _db.Lessons
            .Include(l => l.Exercises)
            .FirstOrDefaultAsync(l => l.Id == command.LessonId, ct)
            ?? throw new NotFoundException($"Lesson with ID '{command.LessonId}' not found.");

        var req = command.Request;
        if (!Enum.TryParse<ExerciseType>(req.Type, ignoreCase: true, out var exerciseType))
            throw new ArgumentException($"Invalid ExerciseType '{req.Type}'.");

        if (!Enum.TryParse<GradingMode>(req.GradingMode, ignoreCase: true, out var gradingMode))
            throw new ArgumentException($"Invalid GradingMode '{req.GradingMode}'.");

        string contentJson = req.Content?.ToJsonString() ?? "{}";
        string? gradingJson = req.Grading?.ToJsonString();
        string? skillTagsJson = req.SkillTags != null && req.SkillTags.Count > 0
            ? JsonSerializer.Serialize(req.SkillTags)
            : null;

        lesson.AddExercise(
            key: req.Key,
            type: exerciseType,
            title: req.Title,
            instructions: req.Instructions,
            maxPoints: req.MaxPoints,
            gradingMode: gradingMode,
            contentJson: contentJson,
            gradingDefinitionJson: gradingJson,
            skillTagsJson: skillTagsJson,
            order: req.Order);

        await _db.SaveChangesAsync(ct);

        var dto = await _getLessonByIdHandler.HandleAsync(new GetLessonByIdQuery(lesson.Id, IsLearner: false), ct);
        return (LessonDetailAuthorDto)dto!;
    }

    public async Task<LessonDetailAuthorDto> HandleAsync(UpdateDraftExerciseCommand command, CancellationToken ct = default)
    {
        var lesson = await _db.Lessons
            .Include(l => l.Exercises)
            .FirstOrDefaultAsync(l => l.Id == command.LessonId, ct)
            ?? throw new NotFoundException($"Lesson with ID '{command.LessonId}' not found.");

        var req = command.Request;
        if (!Enum.TryParse<ExerciseType>(req.Type, ignoreCase: true, out var exerciseType))
            throw new ArgumentException($"Invalid ExerciseType '{req.Type}'.");

        if (!Enum.TryParse<GradingMode>(req.GradingMode, ignoreCase: true, out var gradingMode))
            throw new ArgumentException($"Invalid GradingMode '{req.GradingMode}'.");

        string contentJson = req.Content?.ToJsonString() ?? "{}";
        string? gradingJson = req.Grading?.ToJsonString();
        string? skillTagsJson = req.SkillTags != null && req.SkillTags.Count > 0
            ? JsonSerializer.Serialize(req.SkillTags)
            : null;

        lesson.UpdateExercise(
            exerciseId: command.ExerciseId,
            key: req.Key,
            type: exerciseType,
            title: req.Title,
            instructions: req.Instructions,
            maxPoints: req.MaxPoints,
            gradingMode: gradingMode,
            contentJson: contentJson,
            gradingDefinitionJson: gradingJson,
            skillTagsJson: skillTagsJson);

        await _db.SaveChangesAsync(ct);

        var dto = await _getLessonByIdHandler.HandleAsync(new GetLessonByIdQuery(lesson.Id, IsLearner: false), ct);
        return (LessonDetailAuthorDto)dto!;
    }

    public async Task<LessonDetailAuthorDto> HandleAsync(RemoveDraftExerciseCommand command, CancellationToken ct = default)
    {
        var lesson = await _db.Lessons
            .Include(l => l.Exercises)
            .FirstOrDefaultAsync(l => l.Id == command.LessonId, ct)
            ?? throw new NotFoundException($"Lesson with ID '{command.LessonId}' not found.");

        lesson.RemoveExercise(command.ExerciseId);
        await _db.SaveChangesAsync(ct);

        var dto = await _getLessonByIdHandler.HandleAsync(new GetLessonByIdQuery(lesson.Id, IsLearner: false), ct);
        return (LessonDetailAuthorDto)dto!;
    }

    public async Task<LessonDetailAuthorDto> HandleAsync(ReorderDraftExercisesCommand command, CancellationToken ct = default)
    {
        var lesson = await _db.Lessons
            .Include(l => l.Exercises)
            .FirstOrDefaultAsync(l => l.Id == command.LessonId, ct)
            ?? throw new NotFoundException($"Lesson with ID '{command.LessonId}' not found.");

        lesson.ReorderExercises(command.Request.OrderedExerciseIds);
        await _db.SaveChangesAsync(ct);

        var dto = await _getLessonByIdHandler.HandleAsync(new GetLessonByIdQuery(lesson.Id, IsLearner: false), ct);
        return (LessonDetailAuthorDto)dto!;
    }
}
