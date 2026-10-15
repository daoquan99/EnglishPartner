using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.Vocabulary;
using EnglishLiveTutor.Domain.Learning;
using EnglishLiveTutor.Domain.Learning.Repositories;

namespace EnglishLiveTutor.Application.Features.Learning.Commands.SubmitVocabularyReview;

public sealed record SubmitVocabularyReviewCommand(
    string UserId,
    Guid? DailyItemId,
    SubmitVocabularyReviewRequest Request
) : ICommand<VocabularyFlashcardDto>;
