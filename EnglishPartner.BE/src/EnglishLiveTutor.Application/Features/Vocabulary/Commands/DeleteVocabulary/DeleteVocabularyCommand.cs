using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Vocabulary.Repositories;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Commands.DeleteVocabulary;

public sealed record DeleteVocabularyCommand(Guid Id) : ICommand<bool>;
