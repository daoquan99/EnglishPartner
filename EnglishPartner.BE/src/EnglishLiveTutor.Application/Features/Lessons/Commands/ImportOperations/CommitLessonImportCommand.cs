using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Common.Utilities;
using EnglishLiveTutor.Application.Features.Lessons.Queries.GetLessonById;
using EnglishLiveTutor.Domain.Lessons;
using EnglishLiveTutor.Domain.Lessons.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Lessons.Commands.ImportOperations;

public sealed record CommitLessonImportCommand(
    CommitLessonImportRequest Request,
    string? OperationKey = null
) : ICommand<LessonDetailAuthorDto>, IIdempotentCommand
{
    public string? IdempotencyKey => OperationKey;
}

public sealed class CommitLessonImportCommandHandler : ICommandHandler<CommitLessonImportCommand, LessonDetailAuthorDto>
{
    private readonly IAppDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILessonCandidateValidator _validator;
    private readonly IQueryHandler<GetLessonByIdQuery, object?> _getLessonByIdHandler;
    private readonly ICurrentUser _currentUser;
    private readonly IIdempotencyService _idempotency;

    public CommitLessonImportCommandHandler(
        IAppDbContext db,
        IUnitOfWork unitOfWork,
        ILessonCandidateValidator validator,
        IQueryHandler<GetLessonByIdQuery, object?> getLessonByIdHandler,
        ICurrentUser currentUser,
        IIdempotencyService idempotency)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _getLessonByIdHandler = getLessonByIdHandler;
        _currentUser = currentUser;
        _idempotency = idempotency;
    }

    public async Task<LessonDetailAuthorDto> HandleAsync(CommitLessonImportCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        string userId = _currentUser.UserId ?? "author";
        string scope = req.TargetLessonId?.ToString() ?? "NewLesson";

        // 1. Check idempotency replay
        string fingerprint = IdempotencyFingerprint.Compute(req);
        var replay = await _idempotency.CheckOrBeginOperationAsync(
            userId: userId,
            operationType: "Lesson.CommitImport",
            resourceScope: scope,
            operationKey: command.OperationKey ?? string.Empty,
            requestFingerprint: fingerprint,
            ct: ct);

        if (replay != null)
        {
            if (replay.StatusCode == 409)
            {
                throw new ConflictException(replay.ErrorMessage ?? "Conflict processing lesson commit import.");
            }

            var cached = System.Text.Json.JsonSerializer.Deserialize<LessonDetailAuthorDto>(replay.ResponsePayloadJson!);
            return cached!;
        }

        // 2. Validate candidate thoroughly (never trust IsValid from client!)
        var validation = await _validator.ValidateCandidateAsync(req.Candidate, ct);
        if (!validation.IsValid)
        {
            if (!string.IsNullOrWhiteSpace(command.OperationKey))
            {
                await _idempotency.FailOperationAsync(userId, "Lesson.CommitImport", scope, command.OperationKey, CancellationToken.None);
            }
            throw new InvalidOperationException($"Candidate validation failed with {validation.Issues.Count} issues: {string.Join("; ", validation.Issues.Select(i => i.Message))}");
        }

        var candidateObj = req.Candidate.AsObject();
        var lessonNode = candidateObj["lesson"]
            ?? throw new ArgumentException("Candidate payload missing 'lesson' object.");
        var exercisesArray = candidateObj["exercises"]?.AsArray()
            ?? throw new ArgumentException("Candidate payload missing 'exercises' array.");

        LessonEntity lesson;
        if (req.TargetLessonId.HasValue && req.TargetLessonId.Value != Guid.Empty)
        {
            lesson = await _db.Lessons
                .Include(l => l.Exercises)
                .FirstOrDefaultAsync(l => l.Id == req.TargetLessonId.Value, ct)
                ?? throw new NotFoundException($"Target lesson with ID '{req.TargetLessonId.Value}' not found.");

            if (req.TargetRevision.HasValue && lesson.Version != req.TargetRevision.Value)
            {
                if (!string.IsNullOrWhiteSpace(command.OperationKey))
                {
                    await _idempotency.FailOperationAsync(userId, "Lesson.CommitImport", scope, command.OperationKey, CancellationToken.None);
                }
                throw new ConflictException($"CONCURRENCY_CONFLICT: Target lesson version has changed (current: {lesson.Version}, targetRevision: {req.TargetRevision.Value}).");
            }

            if (string.Equals(req.CommitMode, "Replace", StringComparison.OrdinalIgnoreCase))
            {
                var existingExIds = lesson.Exercises.Select(e => e.Id).ToList();
                foreach (var id in existingExIds)
                {
                    lesson.RemoveExercise(id);
                }
            }
        }
        else
        {
            string title = lessonNode["title"]?.ToString() ?? "Imported Lesson";
            string cefr = lessonNode["cefrLevel"]?.ToString() ?? "B1";
            string topic = lessonNode["topic"]?.ToString() ?? "General";
            string? content = lessonNode["content"]?.ToString();
            int estimatedMinutes = lessonNode["estimatedMinutes"]?.GetValue<int>() ?? 30;

            string objectivesJson = lessonNode["learningObjectives"]?.ToJsonString() ?? "[]";
            string vocabJson = lessonNode["targetVocabulary"]?.ToJsonString() ?? "[]";
            string grammarJson = lessonNode["targetGrammar"]?.ToJsonString() ?? "[]";

            lesson = LessonEntity.Create(
                title: title,
                cefrLevel: cefr,
                topic: topic,
                content: content,
                estimatedMinutes: estimatedMinutes,
                learningObjectivesJson: objectivesJson,
                targetVocabularyJson: vocabJson,
                targetGrammarJson: grammarJson);

            _db.Lessons.Add(lesson);
        }

        foreach (var ex in exercisesArray)
        {
            string key = ex?["key"]?.ToString() ?? Guid.NewGuid().ToString("N")[..8];
            string typeStr = ex?["type"]?.ToString() ?? "Flashcards";
            string title = ex?["title"]?.ToString() ?? key;
            string instructions = ex?["instructions"]?.ToString() ?? string.Empty;
            decimal maxPoints = ex?["maxPoints"]?.GetValue<decimal>() ?? 1.0m;
            string gradingModeStr = ex?["gradingMode"]?.ToString() ?? "Deterministic";

            Enum.TryParse<ExerciseType>(typeStr, ignoreCase: true, out var exerciseType);
            Enum.TryParse<GradingMode>(gradingModeStr, ignoreCase: true, out var gradingMode);

            string contentJson = ex?["content"]?.ToJsonString() ?? "{}";
            string? gradingJson = ex?["grading"]?.ToJsonString();
            string? skillTagsJson = ex?["skillTags"]?.ToJsonString();

            lesson.AddExercise(
                key: key,
                type: exerciseType,
                title: title,
                instructions: instructions,
                maxPoints: maxPoints,
                gradingMode: gradingMode,
                contentJson: contentJson,
                gradingDefinitionJson: gradingJson,
                skillTagsJson: skillTagsJson);
        }

        try
        {
            await _unitOfWork.CommitChangesAsync(ct);

            var dto = await _getLessonByIdHandler.HandleAsync(new GetLessonByIdQuery(lesson.Id, IsLearner: false), ct);
            var result = (LessonDetailAuthorDto)dto!;

            if (!string.IsNullOrWhiteSpace(command.OperationKey))
            {
                await _idempotency.CompleteOperationAsync(
                    userId: userId,
                    operationType: "Lesson.CommitImport",
                    resourceScope: scope,
                    operationKey: command.OperationKey,
                    statusCode: 200,
                    responsePayload: result,
                    ct: ct);
            }

            return result;
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(command.OperationKey))
            {
                await _idempotency.FailOperationAsync(userId, "Lesson.CommitImport", scope, command.OperationKey, CancellationToken.None);
            }
            throw;
        }
    }
}
