using System.Text.Json;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Domain.Grammar.Enums;
using EnglishLiveTutor.Application.Modules.Grammar;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.Admin.PromoteGrammarExercise;

public sealed class PromoteGrammarExerciseCommandHandler
    : ICommandHandler<PromoteGrammarExerciseCommand, GrammarExerciseAdminDto>
{
    private readonly IGrammarDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _timeProvider;

    public PromoteGrammarExerciseCommandHandler(
        IGrammarDbContext db,
        IUnitOfWork uow,
        TimeProvider timeProvider)
    {
        _db = db;
        _uow = uow;
        _timeProvider = timeProvider;
    }

    public async Task<GrammarExerciseAdminDto> HandleAsync(
        PromoteGrammarExerciseCommand command,
        CancellationToken ct = default)
    {
        var exercise = await _db.GrammarExercises
            .FirstOrDefaultAsync(e => e.Id == command.Id && !e.IsDeleted, ct);

        if (exercise == null)
        {
            throw new NotFoundException($"Exercise '{command.Id}' không tồn tại.");
        }

        if (exercise.Moderation != GrammarModerationStatus.Approved)
        {
            throw new ConflictException($"Cannot promote exercise with moderation status '{exercise.Moderation}'. Must be Approved.");
        }

        var step = await _db.GrammarSteps.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == exercise.StepId && !s.IsDeleted, ct);

        if (step == null)
        {
            throw new NotFoundException($"Step for exercise '{command.Id}' does not exist.");
        }

        var existingKeys = (await _db.GrammarExercises.AsNoTracking().IgnoreQueryFilters()
            .Where(e => e.StepId == exercise.StepId && e.Key != null)
            .Select(e => e.Key!)
            .ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        int counter = 1;
        string key;
        do
        {
            key = $"{step.Code}-E{counter:D2}";
            counter++;
        } while (existingKeys.Contains(key));

        var now = _timeProvider.GetUtcNow();
        exercise.PromoteToOfficial(key, command.Request.IsCore, now);

        await _uow.CommitChangesAsync(ct);

        JsonElement contentElem;
        using (var doc = JsonDocument.Parse(exercise.ContentJson))
        {
            contentElem = doc.RootElement.Clone();
        }

        JsonElement? gradingElem = null;
        if (!string.IsNullOrWhiteSpace(exercise.GradingDefinitionJson))
        {
            using var doc = JsonDocument.Parse(exercise.GradingDefinitionJson);
            gradingElem = doc.RootElement.Clone();
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
            Content: contentElem,
            Grading: gradingElem,
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
