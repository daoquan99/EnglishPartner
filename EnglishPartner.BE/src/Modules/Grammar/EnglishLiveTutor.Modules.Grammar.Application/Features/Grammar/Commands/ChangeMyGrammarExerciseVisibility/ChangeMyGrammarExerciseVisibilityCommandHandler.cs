using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.Grammar.Common;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Domain.Grammar.Enums;
using EnglishLiveTutor.Application.Modules.Grammar;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.ChangeMyGrammarExerciseVisibility;

public sealed class ChangeMyGrammarExerciseVisibilityCommandHandler
    : ICommandHandler<ChangeMyGrammarExerciseVisibilityCommand, GrammarMyExerciseDto>
{
    private readonly IGrammarDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public ChangeMyGrammarExerciseVisibilityCommandHandler(
        IGrammarDbContext db,
        IUnitOfWork uow,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _db = db;
        _uow = uow;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<GrammarMyExerciseDto> HandleAsync(
        ChangeMyGrammarExerciseVisibilityCommand command,
        CancellationToken ct = default)
    {
        string userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Bạn cần đăng nhập để đổi trạng thái bài tập.");

        var exercise = await _db.GrammarExercises
            .FirstOrDefaultAsync(e => e.Id == command.Id && !e.IsDeleted, ct);

        // Rule 2: Not found or not owner -> 404
        if (exercise == null || !string.Equals(exercise.OwnerUserId, userId, StringComparison.Ordinal))
        {
            throw new NotFoundException($"Exercise '{command.Id}' không tồn tại hoặc bạn không có quyền truy cập.");
        }

        // Pending limit check if changing to Public
        if (command.Request.Visibility == GrammarExerciseVisibility.Public &&
            exercise.Visibility != GrammarExerciseVisibility.Public)
        {
            int currentPending = await _db.GrammarExercises.AsNoTracking()
                .CountAsync(e => e.OwnerUserId == userId && !e.IsDeleted && e.Moderation == GrammarModerationStatus.Pending, ct);

            if (currentPending >= 30)
            {
                throw new ConflictException("grammar_pending_limit");
            }
        }

        var now = _timeProvider.GetUtcNow();
        exercise.ChangeVisibility(command.Request.Visibility, now);

        await _uow.CommitChangesAsync(ct);

        string stepCode = await _db.GrammarSteps.AsNoTracking()
            .Where(s => s.Id == exercise.StepId)
            .Select(s => s.Code)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        return GrammarExerciseDtoMapper.MapToMyExerciseDto(exercise, stepCode);
    }
}
