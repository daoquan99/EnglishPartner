using EnglishLiveTutor.Application.Common.Progress;
using EnglishLiveTutor.Application.Features.Identity.Contracts;
using EnglishLiveTutor.Domain.Tests.Infrastructure;
using EnglishLiveTutor.Application.Features.Conversations.Contracts;
using EnglishLiveTutor.Application.Features.Grammar.Contracts;
using EnglishLiveTutor.Application.Features.Learning.Contracts;
using EnglishLiveTutor.Application.Features.Lessons.Contracts;
using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Domain.Tests;
using EnglishLiveTutor.Application.Features.Progress.Queries.GetProgressOverview;
using Xunit;

namespace EnglishLiveTutor.Api.IntegrationTests.Progress;

public class GetProgressOverviewQueryHandlerTests
{
    private class FakeUserDirectory(string? timeZoneId) : IUserDirectory
    {
        public List<string> ReceivedUserIds { get; } = new();

        public Task<UserLearningProfile?> GetLearningProfileAsync(string userId, CancellationToken ct = default)
        {
            ReceivedUserIds.Add(userId);
            return Task.FromResult<UserLearningProfile?>(new UserLearningProfile(timeZoneId, "B1"));
        }

        public Task<IReadOnlyList<UserNameInfo>> ListUsersByIdOrUsernameAsync(IReadOnlyCollection<string> idsOrUsernames, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<UserNameInfo>>(Array.Empty<UserNameInfo>());

        public Task<IReadOnlyList<UserNameInfo>> ListUsersByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<UserNameInfo>>(Array.Empty<UserNameInfo>());

        public Task<IReadOnlyList<UserNameInfo>> ListUsersByUsernamesAsync(IReadOnlyCollection<string> usernames, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<UserNameInfo>>(Array.Empty<UserNameInfo>());
    }

    private class FakeVocabularyReader(IReadOnlyList<DailyActivityCount>? activities = null) : IVocabularyProgressReader
    {
        public (DateOnly From, DateOnly To, int Offset)? LastWindow { get; private set; }

        public List<string> DailyActivityUserIds { get; } = new();
        public List<string> SummaryUserIds { get; } = new();

        public Task<IReadOnlyList<DailyActivityCount>> GetDailyActivityAsync(string userId, DateOnly fromLocal, DateOnly toLocal, int utcOffsetMinutes, CancellationToken ct)
        {
            DailyActivityUserIds.Add(userId);
            LastWindow = (fromLocal, toLocal, utcOffsetMinutes);
            return Task.FromResult(activities ?? Array.Empty<DailyActivityCount>());
        }

        public Task<VocabularyProgressSummary> GetSummaryAsync(string userId, DateTimeOffset fromUtc, CancellationToken ct)
        {
            SummaryUserIds.Add(userId);
            return Task.FromResult(new VocabularyProgressSummary(10, 5, 2, 75.5));
        }
    }

    private class FakeLessonsReader(IReadOnlyList<DailyActivityCount>? activities = null) : ILessonsProgressReader
    {
        public List<string> DailyActivityUserIds { get; } = new();
        public List<string> SummaryUserIds { get; } = new();

        public Task<IReadOnlyList<DailyActivityCount>> GetDailyActivityAsync(string userId, DateOnly fromLocal, DateOnly toLocal, int utcOffsetMinutes, CancellationToken ct)
        {
            DailyActivityUserIds.Add(userId);
            return Task.FromResult(activities ?? Array.Empty<DailyActivityCount>());
        }

        public Task<LessonProgressSummary> GetSummaryAsync(string userId, DateTimeOffset fromUtc, CancellationToken ct)
        {
            SummaryUserIds.Add(userId);
            return Task.FromResult(new LessonProgressSummary(4, 90m));
        }
    }

    private class FakeGrammarReader(IReadOnlyList<DailyActivityCount>? activities = null) : IGrammarProgressReader
    {
        public List<string> DailyActivityUserIds { get; } = new();
        public List<string> SummaryUserIds { get; } = new();

        public Task<IReadOnlyList<DailyActivityCount>> GetDailyActivityAsync(string userId, DateOnly fromLocal, DateOnly toLocal, int utcOffsetMinutes, CancellationToken ct)
        {
            DailyActivityUserIds.Add(userId);
            return Task.FromResult(activities ?? Array.Empty<DailyActivityCount>());
        }

        public Task<GrammarProgressSummary> GetSummaryAsync(string userId, DateTimeOffset fromUtc, CancellationToken ct)
        {
            SummaryUserIds.Add(userId);
            return Task.FromResult(new GrammarProgressSummary([new GrammarLevelProgress("A1", 3, 10)], "A1-04"));
        }
    }

    private class FakeSpeechReader(IReadOnlyList<DailyActivityCount>? activities = null) : ISpeechProgressReader
    {
        public List<string> DailyActivityUserIds { get; } = new();
        public List<string> SummaryUserIds { get; } = new();

        public Task<IReadOnlyList<DailyActivityCount>> GetDailyActivityAsync(string userId, DateOnly fromLocal, DateOnly toLocal, int utcOffsetMinutes, CancellationToken ct)
        {
            DailyActivityUserIds.Add(userId);
            return Task.FromResult(activities ?? Array.Empty<DailyActivityCount>());
        }

        public Task<SpeechProgressSummary> GetSummaryAsync(string userId, DateTimeOffset fromUtc, CancellationToken ct)
        {
            SummaryUserIds.Add(userId);
            return Task.FromResult(new SpeechProgressSummary(12, 85m, 88m));
        }
    }

    private class FakeLiveTutorReader(IReadOnlyList<DailyActivityCount>? activities = null) : ILiveTutorProgressReader
    {
        public List<string> DailyActivityUserIds { get; } = new();
        public List<string> SummaryUserIds { get; } = new();

        public Task<IReadOnlyList<DailyActivityCount>> GetDailyActivityAsync(string userId, DateOnly fromLocal, DateOnly toLocal, int utcOffsetMinutes, CancellationToken ct)
        {
            DailyActivityUserIds.Add(userId);
            return Task.FromResult(activities ?? Array.Empty<DailyActivityCount>());
        }

        public Task<LiveTutorProgressSummary> GetSummaryAsync(string userId, DateTimeOffset fromUtc, CancellationToken ct)
        {
            SummaryUserIds.Add(userId);
            return Task.FromResult(new LiveTutorProgressSummary(6, 2));
        }
    }

    private static (GetProgressOverviewQueryHandler Handler, FakeTimeProvider Time) CreateHandler(
        string? timeZoneId = "Asia/Ho_Chi_Minh",
        IReadOnlyList<DailyActivityCount>? vocabActivities = null,
        IReadOnlyList<DailyActivityCount>? lessonActivities = null)
    {
        // 2026-11-02 03:00:00 UTC = 2026-11-02 10:00:00 (+07:00)
        var fixedTime = new DateTimeOffset(2026, 11, 2, 3, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(fixedTime);

        var handler = new GetProgressOverviewQueryHandler(
            new FakeUserDirectory(timeZoneId),
            new FakeVocabularyReader(vocabActivities),
            new FakeLessonsReader(lessonActivities),
            new FakeGrammarReader(),
            new FakeSpeechReader(),
            new FakeLiveTutorReader(),
            timeProvider);

        return (handler, timeProvider);
    }

    [Fact]
    public async Task Handle_StreakWhenTodayHasActivity()
    {
        // Today is 2026-11-02
        var activities = new List<DailyActivityCount>
        {
            new(new DateOnly(2026, 11, 2), 2),
            new(new DateOnly(2026, 11, 1), 1),
            new(new DateOnly(2026, 10, 31), 3)
        };

        var (handler, _) = CreateHandler(vocabActivities: activities);
        var result = await handler.HandleAsync(new GetProgressOverviewQuery("user-1", 14), CancellationToken.None);

        Assert.Equal(3, result.CurrentStreakDays);
        Assert.Equal(3, result.LongestStreakDays);
    }

    [Fact]
    public async Task Handle_StreakWhenTodayHasNoActivityButYesterdayHas()
    {
        // Today is 2026-11-02 (no activity), yesterday 2026-11-01 has activity
        var activities = new List<DailyActivityCount>
        {
            new(new DateOnly(2026, 11, 1), 2),
            new(new DateOnly(2026, 10, 31), 1)
        };

        var (handler, _) = CreateHandler(vocabActivities: activities);
        var result = await handler.HandleAsync(new GetProgressOverviewQuery("user-1", 14), CancellationToken.None);

        Assert.Equal(2, result.CurrentStreakDays);
        Assert.Equal(2, result.LongestStreakDays);
    }

    [Fact]
    public async Task Handle_StreakIsZero_WhenNeitherTodayNorYesterdayHasActivity()
    {
        // Today 2026-11-02 = 0, Yesterday 2026-11-01 = 0. Past activity on 2026-10-30
        var activities = new List<DailyActivityCount>
        {
            new(new DateOnly(2026, 10, 30), 2)
        };

        var (handler, _) = CreateHandler(vocabActivities: activities);
        var result = await handler.HandleAsync(new GetProgressOverviewQuery("user-1", 14), CancellationToken.None);

        Assert.Equal(0, result.CurrentStreakDays);
        Assert.Equal(1, result.LongestStreakDays);
    }

    [Fact]
    public async Task Handle_LongestStreakGreaterThanCurrent()
    {
        // Past streak of 5 days: 2026-05-01 to 2026-05-05
        // Current streak: 2026-11-02 (today) and 2026-11-01 (yesterday) = 2
        var activities = new List<DailyActivityCount>
        {
            new(new DateOnly(2026, 5, 1), 1),
            new(new DateOnly(2026, 5, 2), 1),
            new(new DateOnly(2026, 5, 3), 1),
            new(new DateOnly(2026, 5, 4), 1),
            new(new DateOnly(2026, 5, 5), 1),
            new(new DateOnly(2026, 11, 1), 1),
            new(new DateOnly(2026, 11, 2), 1)
        };

        var (handler, _) = CreateHandler(vocabActivities: activities);
        var result = await handler.HandleAsync(new GetProgressOverviewQuery("user-1", 30), CancellationToken.None);

        Assert.Equal(2, result.CurrentStreakDays);
        Assert.Equal(5, result.LongestStreakDays);
    }

    [Fact]
    public async Task Handle_ActivityHasExactDaysElements_IncludingEmptyDays()
    {
        var activities = new List<DailyActivityCount>
        {
            new(new DateOnly(2026, 11, 2), 3),
            new(new DateOnly(2026, 10, 25), 1)
        };

        var (handler, _) = CreateHandler(vocabActivities: activities);
        var result = await handler.HandleAsync(new GetProgressOverviewQuery("user-1", 14), CancellationToken.None);

        Assert.Equal(14, result.Days);
        Assert.Equal(14, result.Activity.Count);
        // Verify ascending date order
        for (int i = 0; i < result.Activity.Count - 1; i++)
        {
            Assert.True(string.CompareOrdinal(result.Activity[i].Date, result.Activity[i + 1].Date) < 0);
        }
        // First date is today - 13 = 2026-10-20, last is 2026-11-02
        Assert.Equal("2026-10-20", result.Activity[0].Date);
        Assert.Equal("2026-11-02", result.Activity[^1].Date);
        Assert.Equal(2, result.ActiveDaysInRange);
    }

    [Fact]
    public async Task Handle_InvalidOrNullTimeZone_FallsBackToDefault()
    {
        var (handlerNull, _) = CreateHandler(timeZoneId: null);
        var resultNull = await handlerNull.HandleAsync(new GetProgressOverviewQuery("user-1", 7), CancellationToken.None);
        Assert.Equal("Asia/Ho_Chi_Minh", resultNull.TimeZoneId);

        var (handlerInvalid, _) = CreateHandler(timeZoneId: "Invalid/TimeZone_DoesNotExist");
        var resultInvalid = await handlerInvalid.HandleAsync(new GetProgressOverviewQuery("user-1", 7), CancellationToken.None);
        Assert.Equal("Asia/Ho_Chi_Minh", resultInvalid.TimeZoneId);
    }

    [Fact]
    public async Task Handle_PassesQueryUserIdToAllReadersAndUserDirectory()
    {
        var targetUserId = "expected-user-progress-456";
        var userDirectory = new FakeUserDirectory("Asia/Ho_Chi_Minh");
        var vocabReader = new FakeVocabularyReader();
        var lessonsReader = new FakeLessonsReader();
        var grammarReader = new FakeGrammarReader();
        var speechReader = new FakeSpeechReader();
        var tutorReader = new FakeLiveTutorReader();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 11, 2, 3, 0, 0, TimeSpan.Zero));

        var handler = new GetProgressOverviewQueryHandler(
            userDirectory,
            vocabReader,
            lessonsReader,
            grammarReader,
            speechReader,
            tutorReader,
            timeProvider);

        await handler.HandleAsync(new GetProgressOverviewQuery(targetUserId, 30), CancellationToken.None);

        Assert.Single(userDirectory.ReceivedUserIds);
        Assert.Equal(targetUserId, userDirectory.ReceivedUserIds[0]);

        Assert.Single(vocabReader.DailyActivityUserIds);
        Assert.Equal(targetUserId, vocabReader.DailyActivityUserIds[0]);
        Assert.Single(vocabReader.SummaryUserIds);
        Assert.Equal(targetUserId, vocabReader.SummaryUserIds[0]);

        Assert.Single(lessonsReader.DailyActivityUserIds);
        Assert.Equal(targetUserId, lessonsReader.DailyActivityUserIds[0]);
        Assert.Single(lessonsReader.SummaryUserIds);
        Assert.Equal(targetUserId, lessonsReader.SummaryUserIds[0]);

        Assert.Single(grammarReader.DailyActivityUserIds);
        Assert.Equal(targetUserId, grammarReader.DailyActivityUserIds[0]);
        Assert.Single(grammarReader.SummaryUserIds);
        Assert.Equal(targetUserId, grammarReader.SummaryUserIds[0]);

        Assert.Single(speechReader.DailyActivityUserIds);
        Assert.Equal(targetUserId, speechReader.DailyActivityUserIds[0]);
        Assert.Single(speechReader.SummaryUserIds);
        Assert.Equal(targetUserId, speechReader.SummaryUserIds[0]);

        Assert.Single(tutorReader.DailyActivityUserIds);
        Assert.Equal(targetUserId, tutorReader.DailyActivityUserIds[0]);
        Assert.Single(tutorReader.SummaryUserIds);
        Assert.Equal(targetUserId, tutorReader.SummaryUserIds[0]);
    }
    [Fact]
    public async Task Handle_UsesUserLocalDate_WhenUtcIsStillPreviousDay()
    {
        // 2026-11-02 17:30 UTC = 2026-11-03 00:30 (+07:00): local "today" is already Nov 3.
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 2, 17, 30, 0, TimeSpan.Zero));
        var vocab = new FakeVocabularyReader(new List<DailyActivityCount> { new(new DateOnly(2026, 11, 3), 1) });
        var handler = new GetProgressOverviewQueryHandler(
            new FakeUserDirectory("Asia/Ho_Chi_Minh"), vocab, new FakeLessonsReader(), new FakeGrammarReader(),
            new FakeSpeechReader(), new FakeLiveTutorReader(), time);

        var result = await handler.HandleAsync(new GetProgressOverviewQuery("user-1", 7), CancellationToken.None);

        Assert.Equal("2026-11-03", result.Activity[^1].Date);
        Assert.Equal("2026-10-28", result.Activity[0].Date);
        Assert.Equal(1, result.Activity[^1].VocabularyReviews);
        Assert.Equal(1, result.CurrentStreakDays);
        Assert.NotNull(vocab.LastWindow);
        Assert.Equal(420, vocab.LastWindow!.Value.Offset);
        Assert.Equal(new DateOnly(2026, 11, 3), vocab.LastWindow.Value.To);
        Assert.Equal(new DateOnly(2026, 11, 3).AddDays(-364), vocab.LastWindow.Value.From);
    }

    [Fact]
    public async Task Handle_UnknownTimeZone_FallsBackToDefault()
    {
        var (handler, _) = CreateHandler(timeZoneId: "Not/AZone");
        var result = await handler.HandleAsync(new GetProgressOverviewQuery("user-1", 7), CancellationToken.None);
        Assert.Contains(result.TimeZoneId, new[] { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" });
    }

    [Fact]
    public async Task Handle_DuplicateDatesFromReader_AreSummed()
    {
        var activities = new List<DailyActivityCount> { new(new DateOnly(2026, 11, 2), 2), new(new DateOnly(2026, 11, 2), 3) };
        var (handler, _) = CreateHandler(vocabActivities: activities);
        var result = await handler.HandleAsync(new GetProgressOverviewQuery("user-1", 7), CancellationToken.None);
        Assert.Equal(5, result.Activity[^1].VocabularyReviews);
    }
}
