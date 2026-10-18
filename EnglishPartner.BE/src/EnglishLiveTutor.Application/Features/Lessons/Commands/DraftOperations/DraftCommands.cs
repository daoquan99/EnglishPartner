using System.Text.Json;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Lessons.Queries.GetLessonById;
using EnglishLiveTutor.Domain.Lessons;
using EnglishLiveTutor.Domain.Lessons.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Lessons.Commands.DraftOperations;

public sealed record CreateLessonDraftCommand(CreateLessonDraftRequest Request) : ICommand<LessonDetailAuthorDto>;

public sealed record UpdateLessonDraftCommand(Guid LessonId, UpdateLessonDraftRequest Request, string? IfMatchHeader = null) : ICommand<LessonDetailAuthorDto>;

public sealed record DeleteLessonDraftCommand(Guid LessonId) : ICommand;

public sealed record PublishLessonCommand(Guid LessonId, PublishLessonRequest Request, string? IfMatchHeader = null) : ICommand<PublishedLessonResultDto>;

public sealed record ArchiveLessonCommand(Guid LessonId) : ICommand;

public sealed record CreateDraftRevisionCommand(Guid LessonId) : ICommand<DraftRevisionResultDto>;

public sealed record RevertLessonToDraftCommand(Guid LessonId) : ICommand<LessonDetailAuthorDto>;

public sealed class DraftCommandHandlers :
    ICommandHandler<CreateLessonDraftCommand, LessonDetailAuthorDto>,
    ICommandHandler<UpdateLessonDraftCommand, LessonDetailAuthorDto>,
    ICommandHandler<DeleteLessonDraftCommand>,
    ICommandHandler<PublishLessonCommand, PublishedLessonResultDto>,
    ICommandHandler<ArchiveLessonCommand>,
    ICommandHandler<CreateDraftRevisionCommand, DraftRevisionResultDto>,
    ICommandHandler<RevertLessonToDraftCommand, LessonDetailAuthorDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IQueryHandler<GetLessonByIdQuery, object?> _getLessonByIdHandler;

    public DraftCommandHandlers(
        IAppDbContext db,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IQueryHandler<GetLessonByIdQuery, object?> getLessonByIdHandler)
    {
        _db = db;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _getLessonByIdHandler = getLessonByIdHandler;
    }

    public async Task<LessonDetailAuthorDto> HandleAsync(CreateLessonDraftCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var lesson = LessonEntity.Create(
            title: req.Title,
            cefrLevel: req.CefrLevel,
            topic: req.Topic,
            content: req.Content,
            estimatedMinutes: req.EstimatedMinutes,
            learningObjectivesJson: SerializeList(req.LearningObjectives),
            targetVocabularyJson: SerializeList(req.TargetVocabulary),
            targetGrammarJson: SerializeList(req.TargetGrammar));

        _db.Lessons.Add(lesson);
        await _db.SaveChangesAsync(ct);

        var dto = await _getLessonByIdHandler.HandleAsync(new GetLessonByIdQuery(lesson.Id, IsLearner: false), ct);
        return (LessonDetailAuthorDto)dto!;
    }

    public async Task<LessonDetailAuthorDto> HandleAsync(UpdateLessonDraftCommand command, CancellationToken ct = default)
    {
        var lesson = await _db.Lessons
            .Include(l => l.Exercises)
            .FirstOrDefaultAsync(l => l.Id == command.LessonId, ct)
            ?? throw new NotFoundException($"Lesson with ID '{command.LessonId}' not found.");

        ValidateConcurrency(lesson.RowVersion, command.Request.RowVersion ?? command.IfMatchHeader);

        var req = command.Request;
        lesson.UpdateDraft(
            title: req.Title,
            cefrLevel: req.CefrLevel,
            topic: req.Topic,
            content: req.Content,
            estimatedMinutes: req.EstimatedMinutes,
            learningObjectivesJson: SerializeList(req.LearningObjectives),
            targetVocabularyJson: SerializeList(req.TargetVocabulary),
            targetGrammarJson: SerializeList(req.TargetGrammar));

        await _db.SaveChangesAsync(ct);

        var dto = await _getLessonByIdHandler.HandleAsync(new GetLessonByIdQuery(lesson.Id, IsLearner: false), ct);
        return (LessonDetailAuthorDto)dto!;
    }

    public async Task HandleAsync(DeleteLessonDraftCommand command, CancellationToken ct = default)
    {
        var lesson = await _db.Lessons
            .FirstOrDefaultAsync(l => l.Id == command.LessonId, ct)
            ?? throw new NotFoundException($"Lesson with ID '{command.LessonId}' not found.");

        if (lesson.Status != LessonStatus.Draft)
            throw new InvalidOperationException($"Only lessons in 'Draft' status can be deleted. Current: '{lesson.Status}'.");

        bool hasAttempts = await _db.LessonAttempts.AnyAsync(a => a.LessonId == command.LessonId, ct);
        if (hasAttempts)
            throw new InvalidOperationException("Cannot delete draft lesson because learning attempts have already been initiated.");

        lesson.SoftDelete(_timeProvider.GetUtcNow(), _currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PublishedLessonResultDto> HandleAsync(PublishLessonCommand command, CancellationToken ct = default)
    {
        var lesson = await _db.Lessons
            .Include(l => l.Exercises)
            .FirstOrDefaultAsync(l => l.Id == command.LessonId, ct)
            ?? throw new NotFoundException($"Lesson with ID '{command.LessonId}' not found.");

        ValidateConcurrency(lesson.RowVersion, command.Request.RowVersion ?? command.IfMatchHeader);

        // Invariant: Verify deterministic exercises have grading definitions
        foreach (var ex in lesson.Exercises)
        {
            if (ex.GradingMode == GradingMode.Deterministic && string.IsNullOrWhiteSpace(ex.GradingDefinitionJson))
            {
                throw new InvalidOperationException($"Exercise '{ex.Key}' is configured as Deterministic but is missing required answer keys / grading definitions.");
            }
        }

        var publishTime = _timeProvider.GetUtcNow();
        lesson.Publish(publishTime);

        await _db.SaveChangesAsync(ct);

        return new PublishedLessonResultDto(
            LessonId: lesson.Id,
            Version: lesson.Version,
            Status: lesson.Status.ToString(),
            PublishedAt: publishTime.UtcDateTime,
            RowVersion: lesson.RowVersion != null ? Convert.ToBase64String(lesson.RowVersion) : null,
            ExerciseCount: lesson.Exercises.Count,
            TotalMaxPoints: lesson.Exercises.Sum(e => e.MaxPoints));
    }

    public async Task HandleAsync(ArchiveLessonCommand command, CancellationToken ct = default)
    {
        var lesson = await _db.Lessons
            .FirstOrDefaultAsync(l => l.Id == command.LessonId, ct)
            ?? throw new NotFoundException($"Lesson with ID '{command.LessonId}' not found.");

        lesson.Archive(_timeProvider.GetUtcNow());
        await _db.SaveChangesAsync(ct);
    }

    public async Task<DraftRevisionResultDto> HandleAsync(CreateDraftRevisionCommand command, CancellationToken ct = default)
    {
        var publishedLesson = await _db.Lessons
            .Include(l => l.Exercises)
            .FirstOrDefaultAsync(l => l.Id == command.LessonId, ct)
            ?? throw new NotFoundException($"Lesson with ID '{command.LessonId}' not found.");

        var revisionId = Guid.NewGuid();
        var revision = publishedLesson.CreateDraftRevision(revisionId, _timeProvider.GetUtcNow());

        _db.Lessons.Add(revision);
        await _db.SaveChangesAsync(ct);

        return new DraftRevisionResultDto(
            DraftLessonId: revision.Id,
            PublishedParentLessonId: publishedLesson.Id,
            Version: revision.Version,
            Status: revision.Status.ToString(),
            RowVersion: revision.RowVersion != null ? Convert.ToBase64String(revision.RowVersion) : null);
    }

    public async Task<LessonDetailAuthorDto> HandleAsync(RevertLessonToDraftCommand command, CancellationToken ct = default)
    {
        var lesson = await _db.Lessons
            .FirstOrDefaultAsync(l => l.Id == command.LessonId, ct)
            ?? throw new NotFoundException($"Lesson with ID '{command.LessonId}' not found.");

        lesson.RevertToDraft();
        await _db.SaveChangesAsync(ct);

        var dto = await _getLessonByIdHandler.HandleAsync(new GetLessonByIdQuery(lesson.Id, IsLearner: false), ct);
        return (LessonDetailAuthorDto)dto!;
    }

    private static void ValidateConcurrency(byte[]? entityRowVersion, string? clientToken)
    {
        if (entityRowVersion == null || string.IsNullOrWhiteSpace(clientToken))
            return;

        string cleanClientToken = clientToken.Trim('\"').Trim();
        string currentServerToken = Convert.ToBase64String(entityRowVersion);

        if (!string.Equals(currentServerToken, cleanClientToken, StringComparison.Ordinal))
        {
            throw new ConflictException("CONCURRENCY_CONFLICT: The lesson has been modified by another process. Please reload and retry.");
        }
    }

    private static string SerializeList(List<string>? list)
    {
        if (list == null || list.Count == 0)
            return "[]";
        return JsonSerializer.Serialize(list);
    }
}
