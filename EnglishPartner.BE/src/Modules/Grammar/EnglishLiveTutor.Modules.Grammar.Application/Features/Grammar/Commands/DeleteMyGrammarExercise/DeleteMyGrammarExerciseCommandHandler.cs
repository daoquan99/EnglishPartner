using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using MediatR;
using EnglishLiveTutor.Application.Modules.Grammar;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.DeleteMyGrammarExercise;

public sealed class DeleteMyGrammarExerciseCommandHandler
    : ICommandHandler<DeleteMyGrammarExerciseCommand, Unit>
{
    private readonly IGrammarDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public DeleteMyGrammarExerciseCommandHandler(
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

    public async Task<Unit> HandleAsync(
        DeleteMyGrammarExerciseCommand command,
        CancellationToken ct = default)
    {
        string userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Bạn cần đăng nhập để xóa bài tập.");

        var exercise = await _db.GrammarExercises
            .FirstOrDefaultAsync(e => e.Id == command.Id && !e.IsDeleted, ct);

        // Rule 2 & Spec: If not found or OwnerUserId != userId (e.g. promoted to Official where OwnerUserId is null) -> 404
        if (exercise == null || !string.Equals(exercise.OwnerUserId, userId, StringComparison.Ordinal))
        {
            throw new NotFoundException($"Exercise '{command.Id}' không tồn tại hoặc bạn không có quyền truy cập.");
        }

        var now = _timeProvider.GetUtcNow();
        exercise.SoftDelete(now);

        await _uow.CommitChangesAsync(ct);

        return Unit.Value;
    }
}
