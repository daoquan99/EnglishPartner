using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Commands.AbandonFallingWordSession;

public sealed record AbandonFallingWordSessionCommand(
    Guid SessionId,
    string? OperationKey = null) : ICommand<FallingWordSessionDto>;
