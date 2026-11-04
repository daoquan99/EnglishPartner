using EnglishLiveTutor.Application.Common.Progress;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Conversations.Contracts;
using EnglishLiveTutor.Application.Features.Grammar.Contracts;
using EnglishLiveTutor.Application.Features.Identity.Contracts;
using EnglishLiveTutor.Application.Features.Learning.Contracts;
using EnglishLiveTutor.Application.Features.Lessons.Contracts;
using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;

namespace EnglishLiveTutor.Application.Features.Progress.Queries.GetProgressOverview;

public sealed class GetProgressOverviewQueryHandler : IQueryHandler<GetProgressOverviewQuery, ProgressOverviewDto>
{
    private const string DefaultTimeZoneId = "Asia/Ho_Chi_Minh";

    private readonly IUserDirectory _userDirectory;
    private readonly IVocabularyProgressReader _vocabularyReader;
    private readonly ILessonsProgressReader _lessonsReader;
    private readonly IGrammarProgressReader _grammarReader;
    private readonly ISpeechProgressReader _speechReader;
    private readonly ILiveTutorProgressReader _tutorReader;
    private readonly TimeProvider _timeProvider;

    public GetProgressOverviewQueryHandler(
        IUserDirectory userDirectory,
        IVocabularyProgressReader vocabularyReader,
        ILessonsProgressReader lessonsReader,
        IGrammarProgressReader grammarReader,
        ISpeechProgressReader speechReader,
        ILiveTutorProgressReader tutorReader,
        TimeProvider? timeProvider = null)
    {
        _userDirectory = userDirectory;
        _vocabularyReader = vocabularyReader;
        _lessonsReader = lessonsReader;
        _grammarReader = grammarReader;
        _speechReader = speechReader;
        _tutorReader = tutorReader;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ProgressOverviewDto> HandleAsync(GetProgressOverviewQuery query, CancellationToken ct = default)
    {
        var profile = await _userDirectory.GetLearningProfileAsync(query.UserId, ct);
        var timeZone = ResolveTimeZone(profile?.TimeZoneId);

        var utcNow = _timeProvider.GetUtcNow();
        var localNow = TimeZoneInfo.ConvertTime(utcNow, timeZone);
        var offset = timeZone.GetUtcOffset(utcNow);
        int utcOffsetMinutes = (int)offset.TotalMinutes;

        var todayLocal = DateOnly.FromDateTime(localNow.DateTime);
        var chartFromLocal = todayLocal.AddDays(-query.Days + 1);
        var streakFromLocal = todayLocal.AddDays(-364); // 365 days including today

        var chartFromUtc = new DateTimeOffset(
            chartFromLocal.Year, chartFromLocal.Month, chartFromLocal.Day, 0, 0, 0, offset).ToUniversalTime();

        // 1. Sequential calls to the 5 readers for the streak window
        var vocabActivity = await _vocabularyReader.GetDailyActivityAsync(query.UserId, streakFromLocal, todayLocal, utcOffsetMinutes, ct);
        var lessonsActivity = await _lessonsReader.GetDailyActivityAsync(query.UserId, streakFromLocal, todayLocal, utcOffsetMinutes, ct);
        var grammarActivity = await _grammarReader.GetDailyActivityAsync(query.UserId, streakFromLocal, todayLocal, utcOffsetMinutes, ct);
        var speechActivity = await _speechReader.GetDailyActivityAsync(query.UserId, streakFromLocal, todayLocal, utcOffsetMinutes, ct);
        var tutorActivity = await _tutorReader.GetDailyActivityAsync(query.UserId, streakFromLocal, todayLocal, utcOffsetMinutes, ct);

        // 2. Sequential calls to the 5 summaries
        var vocabSummary = await _vocabularyReader.GetSummaryAsync(query.UserId, chartFromUtc, ct);
        var lessonsSummary = await _lessonsReader.GetSummaryAsync(query.UserId, chartFromUtc, ct);
        var grammarSummary = await _grammarReader.GetSummaryAsync(query.UserId, chartFromUtc, ct);
        var speechSummary = await _speechReader.GetSummaryAsync(query.UserId, chartFromUtc, ct);
        var tutorSummary = await _tutorReader.GetSummaryAsync(query.UserId, chartFromUtc, ct);

        // 3. Build lookup maps
        var vocabMap = ToDateMap(vocabActivity);
        var lessonsMap = ToDateMap(lessonsActivity);
        var grammarMap = ToDateMap(grammarActivity);
        var speechMap = ToDateMap(speechActivity);
        var tutorMap = ToDateMap(tutorActivity);

        int TotalActivityOn(DateOnly date)
        {
            vocabMap.TryGetValue(date, out var v);
            lessonsMap.TryGetValue(date, out var l);
            grammarMap.TryGetValue(date, out var g);
            speechMap.TryGetValue(date, out var s);
            tutorMap.TryGetValue(date, out var t);
            return v + l + g + s + t;
        }

        // 4. Calculate streak
        int currentStreakDays = 0;
        var checkDate = TotalActivityOn(todayLocal) > 0 ? todayLocal : todayLocal.AddDays(-1);
        while (checkDate >= streakFromLocal && TotalActivityOn(checkDate) > 0)
        {
            currentStreakDays++;
            checkDate = checkDate.AddDays(-1);
        }

        int longestStreakDays = 0;
        int runningStreak = 0;
        for (var date = streakFromLocal; date <= todayLocal; date = date.AddDays(1))
        {
            if (TotalActivityOn(date) > 0)
            {
                runningStreak++;
                if (runningStreak > longestStreakDays)
                {
                    longestStreakDays = runningStreak;
                }
            }
            else
            {
                runningStreak = 0;
            }
        }

        int activeDaysInRange = 0;
        for (var date = chartFromLocal; date <= todayLocal; date = date.AddDays(1))
        {
            if (TotalActivityOn(date) > 0)
            {
                activeDaysInRange++;
            }
        }

        // 5. Activity list for chart window
        var activityList = new List<ProgressDailyActivityDto>(query.Days);
        for (var date = chartFromLocal; date <= todayLocal; date = date.AddDays(1))
        {
            vocabMap.TryGetValue(date, out var v);
            lessonsMap.TryGetValue(date, out var l);
            grammarMap.TryGetValue(date, out var g);
            speechMap.TryGetValue(date, out var s);
            tutorMap.TryGetValue(date, out var t);

            activityList.Add(new ProgressDailyActivityDto(
                Date: date.ToString("yyyy-MM-dd"),
                VocabularyReviews: v,
                LessonAttempts: l,
                GrammarAttempts: g,
                PronunciationAssessments: s,
                TutorSessions: t));
        }

        // 6. Assemble DTOs
        var vocabularyDto = new ProgressVocabularySummaryDto(
            vocabSummary.Studied,
            vocabSummary.Mastered,
            vocabSummary.DueForReviewNow,
            vocabSummary.AverageMastery);

        var lessonsDto = new ProgressLessonSummaryDto(
            lessonsSummary.CompletedAttempts,
            lessonsSummary.AverageScore100);

        var grammarDto = new ProgressGrammarSummaryDto(
            grammarSummary.Levels.Select(lvl => new ProgressGrammarLevelDto(lvl.Level, lvl.PassedSteps, lvl.TotalSteps)).ToList(),
            grammarSummary.CurrentStepCode);

        var speechDto = new ProgressSpeechSummaryDto(
            speechSummary.Assessments,
            speechSummary.AverageScore,
            speechSummary.AverageScoreSinceFrom);

        var tutorDto = new ProgressLiveTutorSummaryDto(
            tutorSummary.Sessions,
            tutorSummary.SessionsSinceFrom);

        return new ProgressOverviewDto(
            TimeZoneId: timeZone.Id,
            Days: query.Days,
            CurrentStreakDays: currentStreakDays,
            LongestStreakDays: longestStreakDays,
            ActiveDaysInRange: activeDaysInRange,
            Activity: activityList,
            Vocabulary: vocabularyDto,
            Lessons: lessonsDto,
            Grammar: grammarDto,
            Speech: speechDto,
            LiveTutor: tutorDto);
    }

    private static Dictionary<DateOnly, int> ToDateMap(IReadOnlyList<DailyActivityCount> activity) =>
        activity.GroupBy(x => x.Date).ToDictionary(g => g.Key, g => g.Sum(x => x.Count));

    // FindSystemTimeZoneById accepts both IANA and Windows ids on .NET 6+.
    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        return TryFindTimeZone(timeZoneId)
            ?? TryFindTimeZone(DefaultTimeZoneId)
            ?? TimeZoneInfo.CreateCustomTimeZone("UTC+07", TimeSpan.FromHours(7), "UTC+07", "UTC+07");
    }

    private static TimeZoneInfo? TryFindTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return null;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException or ArgumentException or System.Security.SecurityException)
        {
            return null;
        }
    }
}
