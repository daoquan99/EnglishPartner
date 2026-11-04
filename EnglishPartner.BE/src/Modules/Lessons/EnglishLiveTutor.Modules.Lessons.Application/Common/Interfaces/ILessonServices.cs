using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Domain.Common.Exercises;

namespace EnglishLiveTutor.Application.Common.Interfaces;

public sealed record DictationScoreResult(
    int ReferenceWordCount,
    int LearnerWordCount,
    int WordLevenshteinDistance,
    decimal WordAccuracy,
    decimal Score100,
    decimal PointsEarned
);

public interface IDictationScorer
{
    DictationScoreResult Score(string referenceTranscript, string learnerTranscript, decimal maxPoints);
}

public interface ILessonSanitizer
{
    string SanitizeContentForLearner(ExerciseType type, string contentJson);
}

public interface IDictationAiReviewer
{
    Task<DictationAiReviewDto> ReviewAsync(
        Guid answerId,
        string referenceTranscript,
        string learnerTranscript,
        LessonAiExecutionConfigDto? aiConfig = null,
        CancellationToken ct = default);
}
