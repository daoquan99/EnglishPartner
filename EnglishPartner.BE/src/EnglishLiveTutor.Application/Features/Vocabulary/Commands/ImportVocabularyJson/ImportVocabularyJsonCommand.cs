using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Commands.ImportVocabularyJson;

public sealed record ImportVocabularyJsonCommand(string UserId, ImportVocabularyJsonRequest Request)
    : ICommand<List<VocabularyFlashcardDto>>;
