using EnglishLiveTutor.Application.Common.Interfaces;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Commands.ExpireFallingWordRounds;

public sealed record ExpireFallingWordRoundsCommand(
    int BatchSize = 50) : ICommand<int>;
