using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Domain.Grammar.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.Admin.ApproveGrammarExercise;

public sealed class ApproveGrammarExerciseCommandHandler
    : ICommandHandler<ApproveGrammarExerciseCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public ApproveGrammarExerciseCommandHandler(
        IAppDbContext db,
        IUnitOfWork uow,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _db = db;
        _uow = uow;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Unit> HandleAsync(
        ApproveGrammarExerciseCommand command,
        CancellationToken ct = default)
    {
        var exercise = await _db.GrammarExercises
            .FirstOrDefaultAsync(e => e.Id == command.Id && !e.IsDeleted, ct);

        if (exercise == null)
        {
            throw new NotFoundException($"Exercise '{command.Id}' không tồn tại.");
        }

        if (exercise.Moderation != GrammarModerationStatus.Pending)
        {
            throw new ConflictException($"Cannot approve exercise in status '{exercise.Moderation}'. Must be Pending.");
        }

        string reviewer = _currentUser.UserIdOrSystem;
        var now = _timeProvider.GetUtcNow();

        exercise.Approve(reviewer, now);
        await _uow.CommitChangesAsync(ct);

        return Unit.Value;
    }
}
