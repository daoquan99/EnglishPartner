using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Domain.Learning;
using EnglishLiveTutor.Domain.LessonAttempts;
using EnglishLiveTutor.Domain.Lessons;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Tests.Infrastructure;
using EnglishLiveTutor.Domain.Vocabulary;
using EnglishLiveTutor.Domain.VocabularyGames;
using EnglishLiveTutor.Domain.VocabularyGames.Enums;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class EndpointOwnershipSecurityTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public EndpointOwnershipSecurityTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PronunciationHistory_WithoutToken_Returns401()
    {
        var anonymousClient = _factory.CreateAnonymousClient();
        var response = await anonymousClient.GetAsync("/api/pronunciation/assessments/history?userId=" + Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PronunciationHistory_UserBQueryingWithUserAParameter_OnlySeesOwnHistory()
    {
        var userAId = Guid.NewGuid();
        var userBId = Guid.NewGuid();
        var vocabId = Guid.NewGuid();
        var senseId = Guid.NewGuid();

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var userA = new UserEntity("userA_" + Guid.NewGuid().ToString("N")[..8], "userA@test.com", "User A", "dummyHash", id: userAId);
            var userB = new UserEntity("userB_" + Guid.NewGuid().ToString("N")[..8], "userB@test.com", "User B", "dummyHash", id: userBId);
            db.Users.AddRange(userA, userB);

            var vocab = VocabularyEntity.Create("challenge", id: vocabId);
            var sense = vocab.AddSense("noun", "a demanding task", "thu thach", id: senseId);
            db.Vocabularies.Add(vocab);

            var assessmentA = new PronunciationAssessmentEntity(
                userId: userAId.ToString(),
                vocabularyId: vocabId,
                senseId: senseId,
                targetType: "Word",
                targetId: null,
                referenceText: "challenge",
                accent: "US",
                modelCode: "gemini-3.8-flash",
                rubricId: Guid.NewGuid(),
                rubricVersion: 1,
                audioRecordingId: Guid.NewGuid(),
                recognizedText: "challenge",
                overallScore: 95.5m,
                confidence: 0.98m,
                status: "Completed",
                completedAt: DateTimeOffset.UtcNow
            );

            var assessmentB = new PronunciationAssessmentEntity(
                userId: userBId.ToString(),
                vocabularyId: vocabId,
                senseId: senseId,
                targetType: "Word",
                targetId: null,
                referenceText: "challenge",
                accent: "US",
                modelCode: "gemini-3.8-flash",
                rubricId: Guid.NewGuid(),
                rubricVersion: 1,
                audioRecordingId: Guid.NewGuid(),
                recognizedText: "challenge",
                overallScore: 70.0m,
                confidence: 0.85m,
                status: "Completed",
                completedAt: DateTimeOffset.UtcNow
            );

            db.PronunciationAssessments.AddRange(assessmentA, assessmentB);
        });

        var clientB = _factory.CreateAuthenticatedClient(userBId, username: "userB", role: "LEARNER");

        // User B passes ?userId={userAId} in query
        var response = await clientB.GetAsync($"/api/pronunciation/assessments/history?userId={userAId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var history = await response.Content.ReadFromJsonAsync<List<PronunciationAssessmentResultDto>>();
        Assert.NotNull(history);

        // History must only contain User B's assessments and never User A's
        Assert.DoesNotContain(history, a => a.OverallScore == 95.5m);
        Assert.All(history, a => Assert.Equal(70.0m, a.OverallScore));
    }

    [Fact]
    public async Task VocabularyFlashcard_WithoutToken_Returns401()
    {
        var anonymousClient = _factory.CreateAnonymousClient();
        var response = await anonymousClient.GetAsync($"/api/vocabularies/{Guid.NewGuid()}/flashcard");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task VocabularyFlashcard_UserBQueryingWithUserAParameter_DoesNotLeakUserAProgress()
    {
        var userAId = Guid.NewGuid();
        var userBId = Guid.NewGuid();
        var vocabId = Guid.NewGuid();
        var senseId = Guid.NewGuid();

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var userA = new UserEntity("userA_" + Guid.NewGuid().ToString("N")[..8], "userA@test.com", "User A", "dummyHash", id: userAId);
            var userB = new UserEntity("userB_" + Guid.NewGuid().ToString("N")[..8], "userB@test.com", "User B", "dummyHash", id: userBId);
            db.Users.AddRange(userA, userB);

            var vocab = VocabularyEntity.Create("persistent", id: vocabId);
            var sense = vocab.AddSense("adjective", "continuing firmly", "kien tri", id: senseId);
            db.Vocabularies.Add(vocab);

            // User A has mastered progress and high pronunciation score
            var progressA = new UserVocabularyProgressEntity(
                userId: userAId.ToString(),
                vocabularyId: vocabId,
                senseId: senseId,
                masteryLevel: 100,
                intervalDays: 30,
                easeFactor: 2.8m,
                learningStatus: "Mastered",
                reviewCount: 15
            );
            var assessmentA = new PronunciationAssessmentEntity(
                userId: userAId.ToString(),
                vocabularyId: vocabId,
                senseId: senseId,
                targetType: "Word",
                targetId: null,
                referenceText: "persistent",
                accent: "US",
                modelCode: "gemini-3.8-flash",
                rubricId: Guid.NewGuid(),
                rubricVersion: 1,
                audioRecordingId: Guid.NewGuid(),
                recognizedText: "persistent",
                overallScore: 99.0m,
                confidence: 0.99m,
                status: "Completed",
                completedAt: DateTimeOffset.UtcNow
            );

            db.UserVocabularyProgresses.Add(progressA);
            db.PronunciationAssessments.Add(assessmentA);
        });

        var clientB = _factory.CreateAuthenticatedClient(userBId, username: "userB", role: "LEARNER");

        // User B calls flashcard endpoint supplying User A's id in query
        var response = await clientB.GetAsync($"/api/vocabularies/{vocabId}/flashcard?userId={userAId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var flashcard = await response.Content.ReadFromJsonAsync<VocabularyFlashcardDto>();
        Assert.NotNull(flashcard);

        // Flashcard progress must NOT show User A's Mastered status, 100% mastery, or 99.0 score
        Assert.NotEqual("Mastered", flashcard.LearningStatus);
        Assert.NotEqual(100, flashcard.MasteryLevel);
        Assert.NotEqual(99.0m, flashcard.LatestPronunciationScore);
        Assert.Equal("New", flashcard.LearningStatus);
        Assert.Null(flashcard.LatestPronunciationScore);
    }

    [Fact]
    public async Task LessonAttempt_UserBCannotAccessUserAAttempt_Returns404()
    {
        var userAId = Guid.NewGuid();
        var userBId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var attemptAId = Guid.NewGuid();

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var userA = new UserEntity("userA_" + Guid.NewGuid().ToString("N")[..8], "userA@test.com", "User A", "dummyHash", id: userAId);
            var userB = new UserEntity("userB_" + Guid.NewGuid().ToString("N")[..8], "userB@test.com", "User B", "dummyHash", id: userBId);
            db.Users.AddRange(userA, userB);

            var lesson = LessonEntity.Create("Grammar Basics", "A1", "Grammar", id: lessonId);
            db.Lessons.Add(lesson);

            var attemptA = new LessonAttemptEntity(lessonId, userAId.ToString(), pinnedLessonVersion: 1, totalMaxPoints: 100m, id: attemptAId);
            db.LessonAttempts.Add(attemptA);
        });

        // Anonymous client -> 401
        var anonymousClient = _factory.CreateAnonymousClient();
        var anonGet = await anonymousClient.GetAsync($"/api/lesson-attempts/{attemptAId}");
        Assert.Equal(HttpStatusCode.Unauthorized, anonGet.StatusCode);

        var clientB = _factory.CreateAuthenticatedClient(userBId, username: "userB", role: "LEARNER", permissions: new[] { "lesson-exercises.attempt" });

        // User B calling GET /api/lesson-attempts/{attemptAId} -> 404
        var getAttemptResponse = await clientB.GetAsync($"/api/lesson-attempts/{attemptAId}");
        Assert.Equal(HttpStatusCode.NotFound, getAttemptResponse.StatusCode);

        // User B calling GET /api/lesson-attempts/{attemptAId}/resume -> 404
        var resumeResponse = await clientB.GetAsync($"/api/lesson-attempts/{attemptAId}/resume");
        Assert.Equal(HttpStatusCode.NotFound, resumeResponse.StatusCode);

        // User B calling POST /api/lesson-attempts/{attemptAId}/complete -> 404
        var completeResponse = await clientB.PostAsync($"/api/lesson-attempts/{attemptAId}/complete", null);
        Assert.Equal(HttpStatusCode.NotFound, completeResponse.StatusCode);
    }

    [Fact]
    public async Task FallingWordGame_UserBCannotOperateOnUserASession_Returns404()
    {
        var userAId = Guid.NewGuid();
        var userBId = Guid.NewGuid();
        var sessionAId = Guid.NewGuid();

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var userA = new UserEntity("userA_" + Guid.NewGuid().ToString("N")[..8], "userA@test.com", "User A", "dummyHash", id: userAId);
            var userB = new UserEntity("userB_" + Guid.NewGuid().ToString("N")[..8], "userB@test.com", "User B", "dummyHash", id: userBId);
            db.Users.AddRange(userA, userB);

            var sessionA = new FallingWordGameSessionEntity(
                userId: userAId.ToString(),
                source: FallingWordGameSource.VocabularyCatalog,
                cefrLevel: "B1",
                playFilter: FallingWordPlayFilter.All,
                timeLimitSeconds: 5,
                requestedCount: 10,
                id: sessionAId
            );
            db.FallingWordGameSessions.Add(sessionA);
        });

        // Anonymous client -> 401
        var anonymousClient = _factory.CreateAnonymousClient();
        var anonGet = await anonymousClient.GetAsync($"/api/vocabulary-games/falling-words/sessions/{sessionAId}");
        Assert.Equal(HttpStatusCode.Unauthorized, anonGet.StatusCode);

        var clientB = _factory.CreateAuthenticatedClient(userBId, username: "userB", role: "LEARNER");

        // User B calling GET /api/vocabulary-games/falling-words/sessions/{sessionAId} -> 404
        var getResponse = await clientB.GetAsync($"/api/vocabulary-games/falling-words/sessions/{sessionAId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        // User B calling POST /api/vocabulary-games/falling-words/sessions/{sessionAId}/rounds/next -> 404
        var nextRoundResponse = await clientB.PostAsync($"/api/vocabulary-games/falling-words/sessions/{sessionAId}/rounds/next", null);
        Assert.Equal(HttpStatusCode.NotFound, nextRoundResponse.StatusCode);

        // User B calling POST /api/vocabulary-games/falling-words/sessions/{sessionAId}/abandon -> 404
        var abandonResponse = await clientB.PostAsync($"/api/vocabulary-games/falling-words/sessions/{sessionAId}/abandon", null);
        Assert.Equal(HttpStatusCode.NotFound, abandonResponse.StatusCode);
    }

    [Fact]
    public async Task SubmitVocabularyReview_IdempotencyReplay_DoesNotLeakUserAItemStatus_ToUserB()
    {
        var userAId = Guid.NewGuid();
        var userBId = Guid.NewGuid();
        var vocabId = Guid.NewGuid();
        var senseId = Guid.NewGuid();
        var planAId = Guid.NewGuid();
        var itemAId = Guid.NewGuid();
        string clientReqId = "b-client-req-" + Guid.NewGuid().ToString("N");

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var userA = new UserEntity("userA_" + Guid.NewGuid().ToString("N")[..8], "userA@test.com", "User A", "dummyHash", id: userAId);
            var userB = new UserEntity("userB_" + Guid.NewGuid().ToString("N")[..8], "userB@test.com", "User B", "dummyHash", id: userBId);
            db.Users.AddRange(userA, userB);

            var vocab = VocabularyEntity.Create("resilience", id: vocabId);
            var sense = vocab.AddSense("noun", "capacity to recover quickly", "su kien cuong", id: senseId);
            db.Vocabularies.Add(vocab);

            // Plan and item for User A with specific type 'Review' and status 'Pending'
            var planA = new DailyVocabularyPlanEntity(userAId.ToString(), DateTime.UtcNow.ToString("yyyy-MM-dd"), id: planAId);
            planA.AddItem(vocabId, senseId, itemType: "Review", id: itemAId);
            db.DailyVocabularyPlans.Add(planA);

            // User B has a recorded review attempt with their own ClientRequestId
            var attemptB = new VocabularyReviewAttemptEntity(
                userId: userBId.ToString(),
                vocabularyId: vocabId,
                senseId: senseId,
                rating: "Good",
                reviewedAt: DateTimeOffset.UtcNow,
                nextReviewAtSnapshot: DateTimeOffset.UtcNow.AddDays(2),
                clientRequestId: clientReqId
            );
            db.VocabularyReviewAttempts.Add(attemptB);
        });

        var clientB = _factory.CreateAuthenticatedClient(userBId, username: "userB", role: "LEARNER");

        // User B sends review replay with their own clientRequestId, but pointing to User A's item ID
        var reviewPayload = new SubmitVocabularyReviewRequest(
            Rating: "Good",
            VocabularyId: vocabId,
            SenseId: senseId,
            ClientRequestId: clientReqId
        );

        var response = await clientB.PostAsJsonAsync($"/api/learning/flashcards/{itemAId}/review", reviewPayload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<VocabularyFlashcardDto>();
        Assert.NotNull(result);

        // Under L3 fix, loading DailyVocabularyItem must verify plan.UserId == userBId.
        // Since itemAId belongs to User A, existingItem is null and defaults to 'New' / 'Completed'.
        // It must NOT leak User A's itemType 'Review' or status 'Pending'.
        Assert.NotEqual("Review", result.ItemType);
        Assert.NotEqual("Pending", result.ItemStatus);
        Assert.Equal("New", result.ItemType);
        Assert.Equal("Completed", result.ItemStatus);
    }
}
