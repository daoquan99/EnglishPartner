using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Validation;
using EnglishLiveTutor.Application.Features.Grammar.Commands;
using EnglishLiveTutor.Application.Features.Grammar.Commands.Admin;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Application.Features.Grammar.Queries;
using EnglishLiveTutor.Application.Features.Grammar.Services;
using EnglishLiveTutor.Application.Features.Identity.Services;
using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Application.Features.Pronunciation.Services;
using EnglishLiveTutor.Domain.Grammar;
using EnglishLiveTutor.Domain.Grammar.Enums;
using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Domain.Common.Exercises;
using EnglishLiveTutor.Domain.Tests.Infrastructure;
using EnglishLiveTutor.Infrastructure.Lessons;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Interceptors;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Pronunciation.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class GrammarSecurityAndAdminTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public GrammarSecurityAndAdminTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public string? UserId { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public IReadOnlyList<string> Roles { get; set; } = [];
        public IReadOnlyList<string> Permissions { get; set; } = [];
        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(UserId);
    }

    private sealed class FakeDictationScorer : IDictationScorer
    {
        public DictationScoreResult Score(string referenceTranscript, string learnerTranscript, decimal maxPoints) =>
            new(5, 5, 0, 1.0m, 100m, maxPoints);
    }

    private sealed class FakeLessonAiGrader : ILessonAiGrader
    {
        public Task<LessonAiGradingOutcome> GradeSubmissionAsync(
            ExerciseType exerciseType,
            string taskInstructions,
            string cefrLevel,
            ScoringRubricSnapshot rubric,
            string learnerSubmissionPayloadJson,
            decimal maxPoints,
            string? taskContextJson = null,
            LessonAiExecutionConfigDto? aiConfig = null,
            CancellationToken ct = default) =>
            Task.FromResult(new LessonAiGradingOutcome(true, 100m, maxPoints, [], [], [], null));
    }

    private static string ValidHash(int index = 1) => index.ToString("x").PadLeft(64, '0');

    private static (AppDbContext Db, FakeCurrentUser User, TimeProvider Clock, UnitOfWork Uow, IGrammarLearnerContentSanitizer Sanitizer, DeterministicExerciseGrader Grader) CreateHarness()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .AddInterceptors(new AuditingSaveChangesInterceptor())
            .Options;

        var db = new AppDbContext(options);
        var user = new FakeCurrentUser { UserId = Guid.NewGuid().ToString() };
        var clock = TimeProvider.System;
        var uow = new UnitOfWork(db);
        var sanitizer = new GrammarLearnerContentSanitizer();
        var grader = new DeterministicExerciseGrader(new FakeDictationScorer());

        return (db, user, clock, uow, sanitizer, grader);
    }

    [Fact]
    public async Task Security_UserBCannotViewSubmitOrCompleteUserAAttempt_ThrowsNotFound()
    {
        var (db, userA, clock, uow, sanitizer, grader) = CreateHarness();
        var userB = new FakeCurrentUser { UserId = Guid.NewGuid().ToString() };

        await using (db)
        {
            var step = GrammarStepEntity.Create("A1-01", "A1", 1, "Bài 1", "Step 1", "Tóm tắt", "{}");
            db.GrammarSteps.Add(step);
            await db.SaveChangesAsync();

            var ex = GrammarExerciseEntity.CreateOfficial(
                step.Id, "A1-01-E01", ExerciseType.MultipleChoice, 1, "Ex 1", "Instr",
                "{}", "{}", 10m, GradingMode.Deterministic, isCore: true, contentHash: ValidHash(1), now: DateTimeOffset.UtcNow);
            db.GrammarExercises.Add(ex);

            // User A creates attempt
            var attemptA = GrammarAttemptEntity.Start(userA.UserId!, GrammarAttemptKind.StepCore, step.Id, "A1", [ex.Id], DateTimeOffset.UtcNow);
            db.GrammarAttempts.Add(attemptA);
            await db.SaveChangesAsync();

            // 1. User B tries to view User A's attempt -> NotFoundException (404)
            var getHandler = new GetGrammarAttemptQueryHandler(db, userB, sanitizer);
            await Assert.ThrowsAsync<NotFoundException>(() =>
                getHandler.HandleAsync(new GetGrammarAttemptQuery(attemptA.Id)));

            // 2. User B tries to submit answer to User A's attempt -> NotFoundException (404)
            var submitHandler = new SubmitGrammarAnswerCommandHandler(
                db,
                userB,
                clock,
                grader,
                new FakeLessonAiGrader(),
                uow,
                new ScoringRubricProvider(db));
            using var answerDoc = JsonDocument.Parse("{\"selectedOptionIds\":[\"opt-1\"]}");
            await Assert.ThrowsAsync<NotFoundException>(() =>
                submitHandler.HandleAsync(new SubmitGrammarAnswerCommand(attemptA.Id, ex.Id, answerDoc.RootElement)));

            // 3. User B tries to complete User A's attempt -> NotFoundException (404)
            var completeHandler = new CompleteGrammarAttemptCommandHandler(db, userB, clock, uow);
            await Assert.ThrowsAsync<NotFoundException>(() =>
                completeHandler.HandleAsync(new CompleteGrammarAttemptCommand(attemptA.Id)));
        }
    }

    [Fact]
    public async Task Security_CommunitySource_HidesUserAPrivatePendingRejectedFromUserB()
    {
        var (db, _, _, _, sanitizer, _) = CreateHarness();
        var userAId = Guid.NewGuid().ToString();
        var userB = new FakeCurrentUser { UserId = Guid.NewGuid().ToString() };

        await using (db)
        {
            var step = GrammarStepEntity.Create("A1-01", "A1", 1, "Bài 1", "Step 1", "Tóm tắt", "{}");
            db.GrammarSteps.Add(step);
            await db.SaveChangesAsync();

            // Approved (visible in community)
            var approved = GrammarExerciseEntity.CreateUserOwned(
                step.Id, userAId, GrammarExerciseSource.UserAi, GrammarExerciseVisibility.Public,
                ExerciseType.MultipleChoice, 1, "Approved Ex", "Instr", "{}", null, 10m, GradingMode.Deterministic, ValidHash(1), DateTimeOffset.UtcNow);
            approved.Approve("admin", DateTimeOffset.UtcNow);
            db.GrammarExercises.Add(approved);

            // Pending (not visible to User B in community)
            var pending = GrammarExerciseEntity.CreateUserOwned(
                step.Id, userAId, GrammarExerciseSource.UserAi, GrammarExerciseVisibility.Public,
                ExerciseType.MultipleChoice, 1, "Pending Ex", "Instr", "{}", null, 10m, GradingMode.Deterministic, ValidHash(2), DateTimeOffset.UtcNow);
            db.GrammarExercises.Add(pending);

            // Rejected (not visible to User B in community)
            var rejected = GrammarExerciseEntity.CreateUserOwned(
                step.Id, userAId, GrammarExerciseSource.UserAi, GrammarExerciseVisibility.Public,
                ExerciseType.MultipleChoice, 1, "Rejected Ex", "Instr", "{}", null, 10m, GradingMode.Deterministic, ValidHash(3), DateTimeOffset.UtcNow);
            rejected.Reject("admin", "Low quality", DateTimeOffset.UtcNow);
            db.GrammarExercises.Add(rejected);

            // Private (not visible to User B in community)
            var priv = GrammarExerciseEntity.CreateUserOwned(
                step.Id, userAId, GrammarExerciseSource.UserAi, GrammarExerciseVisibility.Private,
                ExerciseType.MultipleChoice, 1, "Private Ex", "Instr", "{}", null, 10m, GradingMode.Deterministic, ValidHash(4), DateTimeOffset.UtcNow);
            db.GrammarExercises.Add(priv);

            await db.SaveChangesAsync();

            var handler = new ListGrammarExercisesQueryHandler(db, new UserDirectory(db), userB, sanitizer);
            var communityList = await handler.HandleAsync(new ListGrammarExercisesQuery("A1-01", "community"));

            Assert.Single(communityList.Items);
            Assert.Equal("Approved Ex", communityList.Items[0].Title);
        }
    }

    [Fact]
    public async Task Security_LearnerDtoNeverExposesGradingOrAnswers_IncludingReadingSubQuestions()
    {
        var sanitizer = new LessonSanitizer();

        // 1. MultipleChoice content with answers
        string mcContent = "{\"questionText\":\"Q\",\"correctOptionIds\":[\"opt-1\"],\"correctAnswers\":[\"A\"]}";
        string sanitizedMc = sanitizer.SanitizeContentForLearner(ExerciseType.MultipleChoice, mcContent);

        Assert.DoesNotContain("correctOptionIds", sanitizedMc);
        Assert.DoesNotContain("correctAnswers", sanitizedMc);

        // 2. ReadingComprehension with subQuestions containing grading
        string readingContent = """
        {
            "passage": "Once upon a time...",
            "subQuestions": [
                {
                    "id": "q1",
                    "questionKind": "SingleChoice",
                    "questionText": "What happened?",
                    "options": [{"id": "o1", "text": "Something"}],
                    "grading": {
                        "correctOptionIds": ["o1"],
                        "explanation": "Because it says so."
                    }
                },
                {
                    "id": "q2",
                    "questionKind": "FillBlank",
                    "questionText": "Fill in [blank-1]",
                    "grading": {
                        "acceptedAnswers": ["forest", "woods"]
                    }
                }
            ]
        }
        """;

        string sanitizedReading = sanitizer.SanitizeContentForLearner(ExerciseType.ReadingComprehension, readingContent);

        Assert.DoesNotContain("\"grading\"", sanitizedReading);
        Assert.DoesNotContain("correctOptionIds", sanitizedReading);
        Assert.DoesNotContain("acceptedAnswers", sanitizedReading);
        Assert.DoesNotContain("explanation", sanitizedReading);
        Assert.Contains("Once upon a time...", sanitizedReading);
        Assert.Contains("What happened?", sanitizedReading);
    }

    [Fact]
    public async Task Security_HttpEndpoint_LearnerCannotAccessAdminEndpoints_Returns403()
    {
        var learnerToken = _factory.CreateAccessToken(
            userId: Guid.NewGuid(),
            role: "LEARNER",
            permissions: ["grammar.read", "grammar.attempt"]);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", learnerToken);

        var response = await client.GetAsync("/api/admin/grammar/steps");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Security_HttpEndpoint_AnonymousCannotAccessRoadmap_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/api/grammar/roadmap");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_UpdateTheory_InvalidSchema_ThrowsValidationException_400()
    {
        var (db, _, _, uow, _, _) = CreateHarness();
        await using (db)
        {
            var step = GrammarStepEntity.Create("A1-01", "A1", 1, "Bài 1", "Step 1", "Tóm tắt", "{}");
            db.GrammarSteps.Add(step);
            await db.SaveChangesAsync();
            await db.SaveChangesAsync();

            var handler = new UpdateGrammarStepTheoryCommandHandler(db, uow);

            // Invalid theory (missing required objectives and sections)
            using var invalidDoc = JsonDocument.Parse("{\"badField\":\"hello\"}");

            await Assert.ThrowsAsync<ValidationException>(() =>
                handler.HandleAsync(new UpdateGrammarStepTheoryCommand("A1-01", invalidDoc.RootElement)));
        }
    }

    [Fact]
    public async Task Admin_UpdateTheory_SetsTheoryEditedByAdmin_AndIncrementsVersion()
    {
        var (db, _, _, uow, _, _) = CreateHarness();
        await using (db)
        {
            const string initialTheory = "{\"objectives\":[\"Initial\"],\"sections\":[{\"heading\":\"H\",\"explanationVi\":\"E\"}]}";
            var step = GrammarStepEntity.Create("A1-01", "A1", 1, "Bài 1", "Step 1", "Tóm tắt", initialTheory, theoryVersion: 1);
            db.GrammarSteps.Add(step);
            await db.SaveChangesAsync();

            var handler = new UpdateGrammarStepTheoryCommandHandler(db, uow);

            const string validTheory = "{\"objectives\":[\"Updated\"],\"sections\":[{\"heading\":\"H2\",\"explanationVi\":\"E2\"}],\"commonMistakes\":[{\"wrong\":\"W\",\"right\":\"R\",\"explanationVi\":\"E\"}],\"summary\":[\"S\"]}";
            using var validDoc = JsonDocument.Parse(validTheory);

            await handler.HandleAsync(new UpdateGrammarStepTheoryCommand("A1-01", validDoc.RootElement));

            var updatedStep = await db.GrammarSteps.FirstAsync(s => s.Code == "A1-01");
            Assert.True(updatedStep.TheoryEditedByAdmin);
            Assert.Equal(2, updatedStep.TheoryVersion);
            Assert.Contains("Updated", updatedStep.TheoryJson);
        }
    }

    [Fact]
    public async Task Admin_CreateOfficialExercise_DuplicateHash_ThrowsConflict_409()
    {
        var (db, _, clock, uow, _, _) = CreateHarness();
        await using (db)
        {
            var step = GrammarStepEntity.Create("A1-01", "A1", 1, "Bài 1", "Step 1", "Tóm tắt", "{}");
            db.GrammarSteps.Add(step);
            await db.SaveChangesAsync();

            const string content = "{\"questionText\":\"What is this?\",\"options\":[{\"id\":\"opt-1\",\"text\":\"A\"},{\"id\":\"opt-2\",\"text\":\"B\"}]}";
            const string grading = "{\"correctOptionIds\":[\"opt-1\"],\"explanation\":\"E\"}";

            var handler = new CreateOfficialGrammarExerciseCommandHandler(db, clock, uow);

            using var contentDoc = JsonDocument.Parse(content);
            using var gradingDoc = JsonDocument.Parse(grading);

            // 1st create succeeds
            var created = await handler.HandleAsync(new CreateOfficialGrammarExerciseCommand(
                "A1-01", "A1-01-E01", ExerciseType.MultipleChoice, 1, "Ex 1", "Instr", 10m, GradingMode.Deterministic,
                contentDoc.RootElement, gradingDoc.RootElement, IsCore: true));

            Assert.NotNull(created);

            // 2nd create with identical content hash in same step throws ConflictException (409)
            await Assert.ThrowsAsync<ConflictException>(() =>
                handler.HandleAsync(new CreateOfficialGrammarExerciseCommand(
                    "A1-01", "A1-01-E02", ExerciseType.MultipleChoice, 1, "Ex 2", "Instr", 10m, GradingMode.Deterministic,
                    contentDoc.RootElement, gradingDoc.RootElement, IsCore: true)));
        }
    }

    [Fact]
    public async Task Admin_DeleteOfficialExercise_SoftDeletesExercise()
    {
        var (db, _, clock, uow, _, _) = CreateHarness();
        await using (db)
        {
            var step = GrammarStepEntity.Create("A1-01", "A1", 1, "Bài 1", "Step 1", "Tóm tắt", "{}");
            db.GrammarSteps.Add(step);
            await db.SaveChangesAsync();

            var ex = GrammarExerciseEntity.CreateOfficial(
                step.Id, "A1-01-E01", ExerciseType.MultipleChoice, 1, "Ex 1", "Instr", "{}", null, 10m, GradingMode.Deterministic,
                isCore: true, contentHash: ValidHash(1), now: DateTimeOffset.UtcNow);
            db.GrammarExercises.Add(ex);
            await db.SaveChangesAsync();

            var handler = new DeleteOfficialGrammarExerciseCommandHandler(db, clock, uow);
            await handler.HandleAsync(new DeleteOfficialGrammarExerciseCommand(ex.Id));

            var stored = await db.GrammarExercises.IgnoreQueryFilters().FirstAsync(e => e.Id == ex.Id);
            Assert.True(stored.IsDeleted);
            Assert.NotNull(stored.DeletedAtUtc);
        }
    }
}
