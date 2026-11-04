using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Commands.CreateFallingWordSession;

public sealed record CreateFallingWordSessionCommand(
    CreateFallingWordSessionRequest Request,
    string? OperationKey = null) : ICommand<FallingWordSessionDto>;
