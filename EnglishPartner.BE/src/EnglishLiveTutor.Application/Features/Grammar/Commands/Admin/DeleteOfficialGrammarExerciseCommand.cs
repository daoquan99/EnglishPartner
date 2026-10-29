using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Domain.Grammar.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.Admin;

public sealed record DeleteOfficialGrammarExerciseCommand(Guid Id) : ICommand<Unit>;

public sealed class DeleteOfficialGrammarExerciseCommandHandler : ICommandHandler<DeleteOfficialGrammarExerciseCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteOfficialGrammarExerciseCommandHandler(
        IAppDbContext db,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
    {
        _db = db;
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> HandleAsync(DeleteOfficialGrammarExerciseCommand command, CancellationToken ct = default)
    {
        var exercise = await _db.GrammarExercises
            .FirstOrDefaultAsync(e => e.Id == command.Id && !e.IsDeleted && e.Source == GrammarExerciseSource.Official, ct);

        if (exercise == null)
        {
            throw new NotFoundException($"Official grammar exercise with ID '{command.Id}' was not found.");
        }

        exercise.SoftDelete(_timeProvider.GetUtcNow());
        await _unitOfWork.CommitChangesAsync(ct);

        return Unit.Value;
    }
}
