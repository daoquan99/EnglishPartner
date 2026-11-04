using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Lessons.Queries.AttemptQueries;
using EnglishLiveTutor.Application.Features.Lessons.Services;
using EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetPronunciationHistory;
using EnglishLiveTutor.Application.Features.Pronunciation.Services;
using EnglishLiveTutor.Application.Features.Vocabulary.Queries.GetVocabularyFlashcard;
using EnglishLiveTutor.Application.Modules.Speech;
using EnglishLiveTutor.Application.Modules.Vocabulary;
using EnglishLiveTutor.Domain.Learning;
using EnglishLiveTutor.Domain.LessonAttempts;
using EnglishLiveTutor.Domain.Lessons;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Vocabulary;
using EnglishLiveTutor.Domain.Vocabulary.Entities;
using EnglishLiveTutor.Infrastructure.Lessons;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Domain.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class OwnershipAndIdentitySecurityTests
{
    private sealed class TestDbFactory : IAppDbContextFactory
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public TestDbFactory(string dbName)
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
        }

        public AppDbContext Create() => new(_options);
        public Task<IAppDbContext> CreateDbContextAsync(CancellationToken ct = default) =>
            Task.FromResult<IAppDbContext>(Create());
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(UserId);
        public IReadOnlyList<string> Roles { get; set; } = new List<string>();
        public bool HasRole(string role) => Roles.Contains(role);
        public bool HasClaim(string type, string value) => false;
        public string? FindClaimValue(string type) => null;
    }

    [Fact]
    public void DomainEntities_ShouldThrowArgumentException_WhenUserIdIsNullOrWhiteSpace()
    {
        // 1. VocabularyGenerationAuditEntity
        Assert.Throws<ArgumentException>(() =>
            new VocabularyGenerationAuditEntity("", "gemini-3.8-flash", "v1", 5));
        Assert.Throws<ArgumentException>(() =>
            new VocabularyGenerationAuditEntity("   ", "gemini-3.8-flash", "v1", 5));

        // 2. PronunciationAssessmentEntity
        Assert.Throws<ArgumentException>(() =>
            new PronunciationAssessmentEntity("", Guid.NewGuid(), Guid.NewGuid(), "Word", null, "cat", "US", "gemini-3.8-flash", Guid.NewGuid(), 1, Guid.NewGuid(), "cat", 85m, 0.9m, "Completed"));
        Assert.Throws<ArgumentException>(() =>
            new PronunciationAssessmentEntity("   ", Guid.NewGuid(), Guid.NewGuid(), "Word", null, "cat", "US", "gemini-3.8-flash", Guid.NewGuid(), 1, Guid.NewGuid(), "cat", 85m, 0.9m, "Completed"));

        // 3. AudioRecordingEntity
        Assert.Throws<ArgumentException>(() =>
            new AudioRecordingEntity("", "storage/cat.webm", "audio/webm", 1024, 1500));
        Assert.Throws<ArgumentException>(() =>
            new AudioRecordingEntity("   ", "storage/cat.webm", "audio/webm", 1024, 1500));

        // 4. UserVocabularyProgressEntity
        Assert.Throws<ArgumentException>(() =>
            new UserVocabularyProgressEntity("", Guid.NewGuid(), Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() =>
            new UserVocabularyProgressEntity("   ", Guid.NewGuid(), Guid.NewGuid()));

        // 5. DailyVocabularyPlanEntity
        Assert.Throws<ArgumentException>(() =>
            new DailyVocabularyPlanEntity("", "2026-10-28"));
        Assert.Throws<ArgumentException>(() =>
            new DailyVocabularyPlanEntity("   ", "2026-10-28"));

        // 6. VocabularyReviewAttemptEntity
        Assert.Throws<ArgumentException>(() =>
            new VocabularyReviewAttemptEntity("", Guid.NewGuid(), Guid.NewGuid(), "Good", DateTimeOffset.UtcNow, null));
        Assert.Throws<ArgumentException>(() =>
            new VocabularyReviewAttemptEntity("   ", Guid.NewGuid(), Guid.NewGuid(), "Good", DateTimeOffset.UtcNow, null));
    }

    [Fact]
    public async Task PronunciationHistory_UserBCannotSeeUserAData()
    {
        string dbName = Guid.NewGuid().ToString("N");
        var factory = new TestDbFactory(dbName);
        await using var db = factory.Create();

        var vocabId = Guid.NewGuid();
        var senseId = Guid.NewGuid();

        // Assessment for User A
        var assessmentA = new PronunciationAssessmentEntity(
            userId: "user-a",
            vocabularyId: vocabId,
            senseId: senseId,
            targetType: "Word",
            targetId: null,
            referenceText: "apple",
            accent: "US",
            modelCode: "gemini-3.8-flash",
            rubricId: Guid.NewGuid(),
            rubricVersion: 1,
            audioRecordingId: Guid.NewGuid(),
            recognizedText: "apple",
            overallScore: 92m,
            confidence: 0.95m,
            status: "Completed");

        // Assessment for User B
        var assessmentB = new PronunciationAssessmentEntity(
            userId: "user-b",
            vocabularyId: vocabId,
            senseId: senseId,
            targetType: "Word",
            targetId: null,
            referenceText: "banana",
            accent: "US",
            modelCode: "gemini-3.8-flash",
            rubricId: Guid.NewGuid(),
            rubricVersion: 1,
            audioRecordingId: Guid.NewGuid(),
            recognizedText: "banana",
            overallScore: 88m,
            confidence: 0.90m,
            status: "Completed");

        db.PronunciationAssessments.AddRange(assessmentA, assessmentB);
        await db.SaveChangesAsync();

        var handler = new GetPronunciationHistoryQueryHandler(
            new ModuleDbContextFactoryAdapter<ISpeechDbContext>(factory));

        // When User B queries history
        var userBHistory = await handler.HandleAsync(new GetPronunciationHistoryQuery("user-b", null, 20));
        Assert.Single(userBHistory);
        Assert.Equal("banana", userBHistory[0].ExpectedText);
        Assert.Equal(88m, userBHistory[0].OverallScore);

        // When User A queries history
        var userAHistory = await handler.HandleAsync(new GetPronunciationHistoryQuery("user-a", null, 20));
        Assert.Single(userAHistory);
        Assert.Equal("apple", userAHistory[0].ExpectedText);
        Assert.Equal(92m, userAHistory[0].OverallScore);
    }

    [Fact]
    public async Task GetVocabularyFlashcard_UserBCannotSeeUserAProgressAndScores()
    {
        string dbName = Guid.NewGuid().ToString("N");
        var factory = new TestDbFactory(dbName);
        await using var db = factory.Create();

        var vocab = new VocabularyEntity("resilient");
        var sense = vocab.AddSense("adjective", "Able to recover quickly", "Kien cuong", isPrimary: true);
        db.Vocabularies.Add(vocab);

        // Progress for User A: Mastered, 95%
        var progressA = new UserVocabularyProgressEntity(
            userId: "user-a",
            vocabularyId: vocab.Id,
            senseId: sense.Id,
            masteryLevel: 95,
            intervalDays: 10,
            easeFactor: 2.6m,
            learningStatus: "Mastered",
            reviewCount: 5,
            correctCount: 5,
            incorrectCount: 0);
        db.UserVocabularyProgresses.Add(progressA);

        // Pronunciation assessment for User A
        var assessmentA = new PronunciationAssessmentEntity(
            userId: "user-a",
            vocabularyId: vocab.Id,
            senseId: sense.Id,
            targetType: "Word",
            targetId: null,
            referenceText: "resilient",
            accent: "US",
            modelCode: "gemini-3.8-flash",
            rubricId: Guid.NewGuid(),
            rubricVersion: 1,
            audioRecordingId: Guid.NewGuid(),
            recognizedText: "resilient",
            overallScore: 98m,
            confidence: 0.99m,
            status: "Completed");
        db.PronunciationAssessments.Add(assessmentA);

        await db.SaveChangesAsync();

        var handler = new GetVocabularyFlashcardQueryHandler(
            new ModuleDbContextFactoryAdapter<IVocabularyDbContext>(factory),
            new PronunciationScoreReader(db));

        // User B requests the flashcard -> must NOT see User A's progress or pronunciation score
        var cardForUserB = await handler.HandleAsync(new GetVocabularyFlashcardQuery(vocab.Id, sense.Id, "user-b"));
        Assert.NotNull(cardForUserB);
        Assert.Equal("New", cardForUserB.LearningStatus);
        Assert.Equal(0, cardForUserB.MasteryLevel);
        Assert.True(cardForUserB.IsNew);
        Assert.Null(cardForUserB.LatestPronunciationScore);

        // User A requests the flashcard -> sees their own progress and score
        var cardForUserA = await handler.HandleAsync(new GetVocabularyFlashcardQuery(vocab.Id, sense.Id, "user-a"));
        Assert.NotNull(cardForUserA);
        Assert.Equal("Mastered", cardForUserA.LearningStatus);
        Assert.Equal(95, cardForUserA.MasteryLevel);
        Assert.False(cardForUserA.IsNew);
        Assert.Equal(98m, cardForUserA.LatestPronunciationScore);
    }

    [Fact]
    public async Task ListLessonAttempts_OnlyReturnsCurrentUserData_AndThrowsWhenUnauthenticated()
    {
        string dbName = Guid.NewGuid().ToString("N");
        var factory = new TestDbFactory(dbName);
        await using var db = factory.Create();

        var lesson = new LessonEntity("Grammar Basics", "A1", "Grammar 101");
        db.Lessons.Add(lesson);

        var attemptA = new LessonAttemptEntity(lesson.Id, "user-a", pinnedLessonVersion: 1, totalMaxPoints: 100m);
        var attemptB = new LessonAttemptEntity(lesson.Id, "user-b", pinnedLessonVersion: 1, totalMaxPoints: 100m);
        db.LessonAttempts.AddRange(attemptA, attemptB);
        await db.SaveChangesAsync();

        var currentUser = new TestCurrentUser();
        var sanitizer = new LessonSanitizer();
        var handler = new AttemptQueryHandlers(db, currentUser, new LessonAttemptResumeBuilder(db, currentUser, sanitizer));

        // 1. Unauthenticated user -> ForbiddenException
        currentUser.UserId = null;
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.HandleAsync(new ListLessonAttemptsQuery()));

        currentUser.UserId = "   ";
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.HandleAsync(new ListLessonAttemptsQuery()));

        // 2. User B only sees User B's attempts
        currentUser.UserId = "user-b";
        var userBResult = await handler.HandleAsync(new ListLessonAttemptsQuery());
        Assert.Single(userBResult.Items);
        Assert.Equal(attemptB.Id, userBResult.Items[0].AttemptId);

        // 3. User A only sees User A's attempts
        currentUser.UserId = "user-a";
        var userAResult = await handler.HandleAsync(new ListLessonAttemptsQuery());
        Assert.Single(userAResult.Items);
        Assert.Equal(attemptA.Id, userAResult.Items[0].AttemptId);
    }

    [Fact]
    public async Task ResumeLessonAttempt_UserBCannotResumeUserAAttempt()
    {
        string dbName = Guid.NewGuid().ToString("N");
        var factory = new TestDbFactory(dbName);
        await using var db = factory.Create();

        var lesson = new LessonEntity("Grammar Basics", "A1", "Grammar 101");
        db.Lessons.Add(lesson);

        var attemptA = new LessonAttemptEntity(lesson.Id, "user-a", pinnedLessonVersion: 1, totalMaxPoints: 100m);
        db.LessonAttempts.Add(attemptA);
        await db.SaveChangesAsync();

        var currentUser = new TestCurrentUser { UserId = "user-b" };
        var sanitizer = new LessonSanitizer();
        var handler = new AttemptQueryHandlers(db, currentUser, new LessonAttemptResumeBuilder(db, currentUser, sanitizer));

        // User B attempts to resume User A's attempt -> NotFoundException
        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.HandleAsync(new ResumeLessonAttemptQuery(attemptA.Id)));

        // User A resumes their own attempt -> Success
        currentUser.UserId = "user-a";
        var resumed = await handler.HandleAsync(new ResumeLessonAttemptQuery(attemptA.Id));
        Assert.NotNull(resumed);
        Assert.Equal(attemptA.Id, resumed.AttemptId);
    }
}
