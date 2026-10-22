using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Commands.SkipFallingWordRound;

public sealed record SkipFallingWordRoundCommand(
    Guid SessionId,
    Guid RoundId,
    string? OperationKey = null) : ICommand<TerminalFallingWordRoundDto>;
