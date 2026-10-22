using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Commands.SubmitFallingWordAnswer;

public sealed record SubmitFallingWordAnswerCommand(
    Guid SessionId,
    Guid RoundId,
    SubmitFallingWordAnswerRequest Request,
    string? OperationKey = null) : ICommand<SubmitFallingWordAnswerResponse>;
