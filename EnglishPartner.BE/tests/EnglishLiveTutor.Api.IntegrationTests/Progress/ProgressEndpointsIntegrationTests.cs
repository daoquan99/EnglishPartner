using System.Net;
using System.Net.Http.Json;
using EnglishLiveTutor.Domain.Conversations;
using EnglishLiveTutor.Domain.Learning;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Tests.Infrastructure;
using EnglishLiveTutor.Application.Features.Progress.Queries.GetProgressOverview;
using Xunit;

namespace EnglishLiveTutor.Api.IntegrationTests.Progress;

public class ProgressEndpointsIntegrationTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public ProgressEndpointsIntegrationTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProgressOverview_WithoutToken_Returns401()
    {
        var anonymousClient = _factory.CreateAnonymousClient();
        var response = await anonymousClient.GetAsync("/api/progress/overview");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProgressOverview_DaysOutOfRange_Returns400()
    {
        var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), role: "USER");

        var responseTooLow = await client.GetAsync("/api/progress/overview?days=5");
        Assert.Equal(HttpStatusCode.BadRequest, responseTooLow.StatusCode);

        var responseTooHigh = await client.GetAsync("/api/progress/overview?days=91");
        Assert.Equal(HttpStatusCode.BadRequest, responseTooHigh.StatusCode);
    }

    [Fact]
    public async Task GetProgressOverview_CrossUserIsolation_UserAOnlySeesOwnProgress()
    {
        var userAId = Guid.NewGuid();
        var userBId = Guid.NewGuid();
        var nowUtc = DateTimeOffset.UtcNow;

        // Seed data for both user A and user B with different counts across 3 sources:
        // Source 1: LiveTutor (Conversation) -> A: 1, B: 3
        // Source 2: Speech (PronunciationAssessment) -> A: 2, B: 4
        // Source 3: Vocabulary (VocabularyReviewAttempt) -> A: 3, B: 2
        await _factory.ExecuteDbContextAsync(async db =>
        {
            // --- User A ---
            // 1 Conversation
            var convA = new ConversationEntity(
                userId: userAId.ToString(),
                id: null,
                title: "User A Conversation");
            convA.SetCreationAudit(nowUtc);
            db.Conversations.Add(convA);

            // 2 Pronunciation Assessments
            var paA1 = new PronunciationAssessmentEntity(
                userAId.ToString(), Guid.NewGuid(), Guid.NewGuid(), "apple", null, "test", "US", "gemini",
                Guid.NewGuid(), 1, Guid.NewGuid(), "test", 80m, 0.9m, "Completed");
            paA1.SetCreationAudit(nowUtc);

            var paA2 = new PronunciationAssessmentEntity(
                userAId.ToString(), Guid.NewGuid(), Guid.NewGuid(), "banana", null, "test", "US", "gemini",
                Guid.NewGuid(), 1, Guid.NewGuid(), "test", 90m, 0.9m, "Completed");
            paA2.SetCreationAudit(nowUtc);

            db.PronunciationAssessments.AddRange(paA1, paA2);

            // 3 Vocabulary Review Attempts
            for (int i = 0; i < 3; i++)
            {
                var vrA = new VocabularyReviewAttemptEntity(
                    userAId.ToString(), Guid.NewGuid(), Guid.NewGuid(), "Good", nowUtc, null);
                db.VocabularyReviewAttempts.Add(vrA);
            }

            // --- User B ---
            // 3 Conversations
            for (int i = 0; i < 3; i++)
            {
                var convB = new ConversationEntity(
                    userId: userBId.ToString(),
                    id: null,
                    title: $"User B Conversation {i}");
                convB.SetCreationAudit(nowUtc);
                db.Conversations.Add(convB);
            }

            // 4 Pronunciation Assessments
            for (int i = 0; i < 4; i++)
            {
                var paB = new PronunciationAssessmentEntity(
                    userBId.ToString(), Guid.NewGuid(), Guid.NewGuid(), $"word{i}", null, "test", "US", "gemini",
                    Guid.NewGuid(), 1, Guid.NewGuid(), "test", 75m, 0.9m, "Completed");
                paB.SetCreationAudit(nowUtc);
                db.PronunciationAssessments.Add(paB);
            }

            // 2 Vocabulary Review Attempts
            for (int i = 0; i < 2; i++)
            {
                var vrB = new VocabularyReviewAttemptEntity(
                    userBId.ToString(), Guid.NewGuid(), Guid.NewGuid(), "Hard", nowUtc, null);
                db.VocabularyReviewAttempts.Add(vrB);
            }
        });

        var clientA = _factory.CreateAuthenticatedClient(userAId, role: "USER");
        var clientB = _factory.CreateAuthenticatedClient(userBId, role: "USER");

        // User A request
        var responseA = await clientA.GetAsync("/api/progress/overview?days=30");
        Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);
        var dtoA = await responseA.Content.ReadFromJsonAsync<ProgressOverviewDto>();
        Assert.NotNull(dtoA);
        Assert.Equal(1, dtoA.LiveTutor.Sessions);
        Assert.Equal(2, dtoA.Speech.Assessments);
        var todayA = dtoA.Activity[^1];
        Assert.Equal(1, todayA.TutorSessions);
        Assert.Equal(2, todayA.PronunciationAssessments);
        Assert.Equal(3, todayA.VocabularyReviews);
        var totalActivityA = todayA.TutorSessions +
                             todayA.PronunciationAssessments +
                             todayA.VocabularyReviews +
                             todayA.LessonAttempts +
                             todayA.GrammarAttempts;
        Assert.Equal(6, totalActivityA);

        // User B request
        var responseB = await clientB.GetAsync("/api/progress/overview?days=30");
        Assert.Equal(HttpStatusCode.OK, responseB.StatusCode);
        var dtoB = await responseB.Content.ReadFromJsonAsync<ProgressOverviewDto>();
        Assert.NotNull(dtoB);
        Assert.Equal(3, dtoB.LiveTutor.Sessions);
        Assert.Equal(4, dtoB.Speech.Assessments);
        var todayB = dtoB.Activity[^1];
        Assert.Equal(3, todayB.TutorSessions);
        Assert.Equal(4, todayB.PronunciationAssessments);
        Assert.Equal(2, todayB.VocabularyReviews);
        var totalActivityB = todayB.TutorSessions +
                             todayB.PronunciationAssessments +
                             todayB.VocabularyReviews +
                             todayB.LessonAttempts +
                             todayB.GrammarAttempts;
        Assert.Equal(9, totalActivityB);
    }
}
