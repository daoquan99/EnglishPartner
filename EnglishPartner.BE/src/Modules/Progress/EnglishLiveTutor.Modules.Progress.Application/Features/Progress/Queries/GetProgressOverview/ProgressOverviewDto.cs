namespace EnglishLiveTutor.Application.Features.Progress.Queries.GetProgressOverview;

public sealed record ProgressDailyActivityDto(
    string Date,
    int VocabularyReviews,
    int LessonAttempts,
    int GrammarAttempts,
    int PronunciationAssessments,
    int TutorSessions);

public sealed record ProgressVocabularySummaryDto(
    int Studied,
    int Mastered,
    int DueForReviewNow,
    double AverageMastery);

public sealed record ProgressLessonSummaryDto(
    int CompletedAttempts,
    decimal? AverageScore100);

public sealed record ProgressGrammarLevelDto(
    string Level,
    int PassedSteps,
    int TotalSteps);

public sealed record ProgressGrammarSummaryDto(
    IReadOnlyList<ProgressGrammarLevelDto> Levels,
    string? CurrentStepCode);

public sealed record ProgressSpeechSummaryDto(
    int Assessments,
    decimal? AverageScore,
    decimal? AverageScoreInRange);

public sealed record ProgressLiveTutorSummaryDto(
    int Sessions,
    int SessionsInRange);

public sealed record ProgressOverviewDto(
    string TimeZoneId,
    int Days,
    int CurrentStreakDays,
    int LongestStreakDays,
    int ActiveDaysInRange,
    IReadOnlyList<ProgressDailyActivityDto> Activity,
    ProgressVocabularySummaryDto Vocabulary,
    ProgressLessonSummaryDto Lessons,
    ProgressGrammarSummaryDto Grammar,
    ProgressSpeechSummaryDto Speech,
    ProgressLiveTutorSummaryDto LiveTutor);
