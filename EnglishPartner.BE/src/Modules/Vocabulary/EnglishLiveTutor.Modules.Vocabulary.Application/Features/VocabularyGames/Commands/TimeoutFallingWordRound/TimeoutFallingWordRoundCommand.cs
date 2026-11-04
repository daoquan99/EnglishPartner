using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Commands.TimeoutFallingWordRound;

public sealed record TimeoutFallingWordRoundCommand(
    Guid SessionId,
    Guid RoundId,
    string? OperationKey = null) : ICommand<TerminalFallingWordRoundDto>;
