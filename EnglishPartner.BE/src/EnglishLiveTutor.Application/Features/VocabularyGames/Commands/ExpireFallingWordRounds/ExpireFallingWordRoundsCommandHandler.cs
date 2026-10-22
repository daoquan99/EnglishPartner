using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Domain.VocabularyGames.Enums;
using EnglishLiveTutor.Domain.VocabularyGames.Repositories;
using Microsoft.Extensions.Logging;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Commands.ExpireFallingWordRounds;

public sealed class ExpireFallingWordRoundsCommandHandler : ICommandHandler<ExpireFallingWordRoundsCommand, int>
{
    private readonly IFallingWordGameRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ExpireFallingWordRoundsCommandHandler> _logger;

    public ExpireFallingWordRoundsCommandHandler(
        IFallingWordGameRepository repo,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ILogger<ExpireFallingWordRoundsCommandHandler> logger)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<int> HandleAsync(ExpireFallingWordRoundsCommand command, CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow();
        int batchSize = Math.Clamp(command.BatchSize, 1, 200);

        var overdueRounds = await _repo.GetOverdueActiveRoundsAsync(now, batchSize, ct);
        if (overdueRounds.Count == 0)
        {
            return 0;
        }

        int finalizedCount = 0;
        var sessionGroups = overdueRounds.GroupBy(r => r.SessionId);

        foreach (var group in sessionGroups)
        {
            try
            {
                var session = await _repo.GetByIdWithRoundsAsync(group.Key, ct);
                if (session == null ||
                    session.Status == FallingWordGameStatus.Completed ||
                    session.Status == FallingWordGameStatus.Abandoned)
                {
                    continue;
                }

                foreach (var round in group)
                {
                    session.FinalizeTimeout(round.Id, now);
                    finalizedCount++;
                }

                await _unitOfWork.CommitChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tự động xử lý hết giờ cho phiên chơi {SessionId}.", group.Key);
            }
        }

        return finalizedCount;
    }
}
