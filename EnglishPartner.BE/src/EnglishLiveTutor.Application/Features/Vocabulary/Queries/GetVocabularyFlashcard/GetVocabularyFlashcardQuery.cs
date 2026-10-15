using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Queries.GetVocabularyFlashcard;

public sealed record GetVocabularyFlashcardQuery(
    Guid Id,
    Guid? SenseId,
    string UserId = "default-learner"
) : IQuery<VocabularyFlashcardDto?>;
