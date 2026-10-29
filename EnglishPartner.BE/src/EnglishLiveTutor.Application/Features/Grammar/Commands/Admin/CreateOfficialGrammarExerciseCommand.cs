using System.Text.Json;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Common.Validation;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Application.Features.Grammar.Rules;
using EnglishLiveTutor.Domain.Grammar;
using EnglishLiveTutor.Domain.Grammar.Enums;
using EnglishLiveTutor.Domain.Lessons.Enums;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.Admin;

public sealed record CreateOfficialGrammarExerciseCommand(
    string Code,
    string? Key,
    ExerciseType Type,
    int Difficulty,
    string Title,
    string Instructions,
    decimal MaxPoints,
    GradingMode GradingMode,
    JsonElement Content,
    JsonElement? Grading,
    bool IsCore,
    string? Focus = null
) : ICommand<GrammarExerciseAdminDto>;

public sealed class CreateOfficialGrammarExerciseCommandHandler : ICommandHandler<CreateOfficialGrammarExerciseCommand, GrammarExerciseAdminDto>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOfficialGrammarExerciseCommandHandler(
        IAppDbContext db,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
    {
        _db = db;
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<GrammarExerciseAdminDto> HandleAsync(CreateOfficialGrammarExerciseCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Code))
        {
            throw new NotFoundException($"Grammar step code '{command.Code}' not found.");
        }

        string normalizedCode = command.Code.Trim().ToUpperInvariant();

        var step = await _db.GrammarSteps
            .FirstOrDefaultAsync(s => s.Code == normalizedCode && !s.IsDeleted, ct);

        if (step == null)
        {
            throw new NotFoundException($"Grammar step '{normalizedCode}' was not found.");
        }

        string contentJson = command.Content.GetRawText();
        string? gradingJson = command.Grading.HasValue ? command.Grading.Value.GetRawText() : null;

        var validationIssues = GrammarExerciseRules.Validate(
            command.Type,
            contentJson,
            gradingJson,
            command.MaxPoints,
            command.GradingMode);

        var errorIssues = validationIssues.Where(i => string.Equals(i.Severity, "Error", StringComparison.OrdinalIgnoreCase)).ToList();
        if (errorIssues.Count > 0)
        {
            var failures = errorIssues.Select(i => new ValidationFailure(i.Path, i.Message));
            throw new ValidationException(failures);
        }

        string key = command.Key?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(key))
        {
            int existingCount = await _db.GrammarExercises
                .CountAsync(e => e.StepId == step.Id && e.Source == GrammarExerciseSource.Official, ct);

            int nextNum = existingCount + 1;
            do
            {
                key = $"{step.Code}-E{nextNum:D2}";
                nextNum++;
            } while (await _db.GrammarExercises.AnyAsync(e => e.StepId == step.Id && !e.IsDeleted && e.Key == key, ct));
        }
        else
        {
            bool keyExists = await _db.GrammarExercises
                .AnyAsync(e => e.StepId == step.Id && !e.IsDeleted && e.Key == key, ct);

            if (keyExists)
            {
                throw new ConflictException($"An exercise with key '{key}' already exists in step '{step.Code}'.");
            }
        }

        string contentHash = GrammarContentHasher.ComputeHash(command.Type, contentJson, gradingJson);

        bool hashExists = await _db.GrammarExercises
            .AnyAsync(e => e.StepId == step.Id && !e.IsDeleted && e.ContentHash == contentHash &&
                           (e.Source == GrammarExerciseSource.Official || e.Moderation == GrammarModerationStatus.Approved), ct);

        if (hashExists)
        {
            throw new ConflictException($"An exercise with identical content hash already exists in step '{step.Code}'.");
        }

        var now = _timeProvider.GetUtcNow();

        var exercise = GrammarExerciseEntity.CreateOfficial(
            stepId: step.Id,
            key: key,
            type: command.Type,
            difficulty: command.Difficulty,
            title: command.Title,
            instructions: command.Instructions,
            contentJson: contentJson,
            gradingDefinitionJson: gradingJson,
            maxPoints: command.MaxPoints,
            gradingMode: command.GradingMode,
            isCore: command.IsCore,
            contentHash: contentHash,
            now: now,
            focus: command.Focus);

        _db.GrammarExercises.Add(exercise);
        await _unitOfWork.CommitChangesAsync(ct);

        JsonElement contentElement;
        try
        {
            using var doc = JsonDocument.Parse(exercise.ContentJson);
            contentElement = doc.RootElement.Clone();
        }
        catch
        {
            using var doc = JsonDocument.Parse("{}");
            contentElement = doc.RootElement.Clone();
        }

        JsonElement? gradingElement = null;
        if (!string.IsNullOrWhiteSpace(exercise.GradingDefinitionJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(exercise.GradingDefinitionJson);
                gradingElement = doc.RootElement.Clone();
            }
            catch { }
        }

        return new GrammarExerciseAdminDto(
            Id: exercise.Id,
            StepId: exercise.StepId,
            Key: exercise.Key,
            Type: exercise.Type,
            Difficulty: exercise.Difficulty,
            Title: exercise.Title,
            Instructions: exercise.Instructions,
            Focus: exercise.Focus,
            Content: contentElement,
            Grading: gradingElement,
            MaxPoints: exercise.MaxPoints,
            GradingMode: exercise.GradingMode,
            Source: exercise.Source,
            OwnerUserId: exercise.OwnerUserId,
            AuthorUserId: exercise.AuthorUserId,
            Visibility: exercise.Visibility,
            Moderation: exercise.Moderation,
            RejectReason: exercise.RejectReason,
            ReviewedBy: exercise.ReviewedBy,
            ReviewedAtUtc: exercise.ReviewedAtUtc,
            IsCore: exercise.IsCore,
            ContentHash: exercise.ContentHash,
            CreatedAtUtc: exercise.CreatedAtUtc,
            UpdatedAtUtc: exercise.UpdatedAtUtc
        );
    }
}
