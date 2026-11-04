using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Commands.StartNextFallingWordRound;

public sealed record StartNextFallingWordRoundCommand(
    Guid SessionId,
    string? OperationKey = null) : ICommand<ActiveFallingWordRoundDto?>;
