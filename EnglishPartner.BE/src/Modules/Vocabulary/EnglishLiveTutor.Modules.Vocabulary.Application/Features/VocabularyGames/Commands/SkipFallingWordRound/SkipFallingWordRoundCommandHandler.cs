using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;
using EnglishLiveTutor.Application.Features.VocabularyGames.Mapping;
using EnglishLiveTutor.Application.Modules.Vocabulary;
using EnglishLiveTutor.Domain.VocabularyGames.Repositories;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Commands.SkipFallingWordRound;

public sealed class SkipFallingWordRoundCommandHandler(
    IFallingWordGameRepository gameRepository,
    IVocabularyDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<SkipFallingWordRoundCommand, TerminalFallingWordRoundDto>
{
    public async Task<TerminalFallingWordRoundDto> HandleAsync(SkipFallingWordRoundCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(currentUser.UserId))
            throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        var session = await gameRepository.GetByIdWithRoundsForUserAsync(command.SessionId, currentUser.UserId, ct)
            ?? throw new NotFoundException($"Không tìm thấy phiên chơi với ID '{command.SessionId}'.");

        var now = DateTimeOffset.UtcNow;
        var round = session.SkipRound(command.RoundId, now);

        await dbContext.SaveChangesAsync(ct);

        return FallingWordDtoMapper.ToTerminalRoundDto(round);
    }
}
