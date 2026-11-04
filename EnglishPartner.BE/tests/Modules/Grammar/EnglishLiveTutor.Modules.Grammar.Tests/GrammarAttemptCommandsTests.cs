using System.Text.Json;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.Grammar;
using EnglishLiveTutor.Application.Features.Grammar.Commands;
using EnglishLiveTutor.Application.Features.Grammar.Services;
using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Application.Features.Pronunciation.Services;
using EnglishLiveTutor.Domain.Grammar;
using EnglishLiveTutor.Domain.Grammar.Enums;
using EnglishLiveTutor.Domain.Common.Exercises;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Pronunciation.Entities;
using EnglishLiveTutor.Infrastructure.Lessons;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class GrammarAttemptCommandsTests
{
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
        public Func<Task<LessonAiGradingOutcome>>? Handler { get; set; }
        public Action? OnGradeCalled { get; set; }

        public async Task<LessonAiGradingOutcome> GradeSubmissionAsync(
            ExerciseType exerciseType,
            string taskInstructions,
            string cefrLevel,
            ScoringRubricSnapshot rubric,
            string learnerSubmissionPayloadJson,
            decimal maxPoints,
            string? taskContextJson = null,
            LessonAiExecutionConfigDto? aiConfig = null,
            CancellationToken ct = default)
        {
            OnGradeCalled?.Invoke();

            if (Handler != null)
            {
                return await Handler();
            }

            return new LessonAiGradingOutcome(
                IsAssessable: true,
                Score100: 95m,
                PointsEarned: Math.Round(95m / 100m * maxPoints, 2),
                CriteriaBreakdown:
                [
                    new LessonAiCriterionScore("ACCURACY", "Accuracy", 1m, 4, "Excellent", "Great accuracy")
                ],
                Strengths: ["Natural phrasing"],
                Improvements: ["Watch preposition usage"],
                SuggestedAnswer: "He goes to school every day."
            );
        }
    }

    private sealed class CountingUnitOfWork : IUnitOfWork
    {
        private readonly IUnitOfWork _inner;
        public int CommitCount { get; private set; }

        public CountingUnitOfWork(IUnitOfWork inner)
        {
            _inner = inner;
        }

        public async Task<int> CommitChangesAsync(CancellationToken ct = default)
        {
            CommitCount++;
            return await _inner.CommitChangesAsync(ct);
        }

        public Task<TResponse> ExecuteInTransactionAsync<TResponse>(Func<CancellationToken, Task<TResponse>> operation, CancellationToken ct = default) =>
            _inner.ExecuteInTransactionAsync(operation, ct);
    }

    private static string ValidHash(int index = 1) => index.ToString("x").PadLeft(64, '0');

    private static (AppDbContext Db, FakeCurrentUser User, TimeProvider Clock, IUnitOfWork Uow, IGrammarLearnerContentSanitizer Sanitizer, IDeterministicExerciseGrader Grader, FakeLessonAiGrader AiGrader) CreateHarness()
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
        var aiGrader = new FakeLessonAiGrader();

        return (db, user, clock, uow, sanitizer, grader, aiGrader);
    }

    private static void SeedRubrics(AppDbContext db)
    {
        var transRubric = new ScoringRubricEntity(
            code: "LESSON_TRANSLATION",
            name: "Translation Rubric",
            description: "Translation",
            version: 1);

        transRubric.AddCriterion("ACCURACY", "Accuracy", "Accuracy", 1.0m, displayOrder: 1);
        db.ScoringRubrics.Add(transRubric);

        var writingRubric = new ScoringRubricEntity(
            code: "LESSON_WRITING",
            name: "Writing Rubric",
            description: "Writing",
            version: 1);

        writingRubric.AddCriterion("GRAMMAR", "Grammar", "Grammar", 1.0m, displayOrder: 1);
        db.ScoringRubrics.Add(writingRubric);

        db.SaveChanges();
    }

    private static void SeedRoadmapWithExercises(AppDbContext db)
    {
        const string theory = "{\"objectives\":[\"Test\"],\"sections\":[{\"heading\":\"H\",\"explanationVi\":\"E\"}]}";

        for (int i = 1; i <= 20; i++)
        {
            string code = $"A1-{i:D2}";
            var step = GrammarStepEntity.Create(code, "A1", i, $"Bài {code}", $"Step {code}", "Tóm tắt", theory, isLevelReview: i == 20);
            db.GrammarSteps.Add(step);

            // Add 1 core exercise per step
            db.GrammarExercises.Add(GrammarExerciseEntity.CreateOfficial(
                step.Id, $"{code}-E01", ExerciseType.MultipleChoice, 1, $"Core {code}", "Select correct",
                "{\"questionText\":\"Q\",\"options\":[{\"id\":\"opt-1\",\"text\":\"A\"}],\"correctOptionIds\":[\"opt-1\"]}",
                "{\"correctOptionIds\":[\"opt-1\"],\"explanation\":\"Correct!\"}",
                10m, GradingMode.Deterministic, isCore: true, contentHash: ValidHash(i), now: DateTimeOffset.UtcNow));
        }

        // Level A2
        for (int i = 1; i <= 20; i++)
        {
            string code = $"A2-{i:D2}";
            var step = GrammarStepEntity.Create(code, "A2", i, $"Bài {code}", $"Step {code}", "Tóm tắt", theory, isLevelReview: i == 20);
            db.GrammarSteps.Add(step);

            db.GrammarExercises.Add(GrammarExerciseEntity.CreateOfficial(
                step.Id, $"{code}-E01", ExerciseType.MultipleChoice, 1, $"Core {code}", "Select correct",
                "{\"questionText\":\"Q\",\"options\":[{\"id\":\"opt-1\",\"text\":\"A\"}],\"correctOptionIds\":[\"opt-1\"]}",
                "{\"correctOptionIds\":[\"opt-1\"],\"explanation\":\"Correct!\"}",
                10m, GradingMode.Deterministic, isCore: true, contentHash: ValidHash(100 + i), now: DateTimeOffset.UtcNow));
        }

        db.SaveChanges();
    }

    [Fact]
    public async Task StartAttempt_StepLocked_ThrowsForbidden_403()
    {
        var (db, user, clock, uow, sanitizer, _, _) = CreateHarness();
        await using (db)
        {
            SeedRoadmapWithExercises(db);

            var handler = new StartGrammarAttemptCommandHandler(db, user, clock, uow, sanitizer);

            // A1-02 is locked for new user
            var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
                handler.HandleAsync(new StartGrammarAttemptCommand(GrammarAttemptKind.StepCore, "A1-02")));

            Assert.Equal("grammar_step_locked", ex.Message);
        }
    }

    [Fact]
    public async Task StartAttempt_StepCore_HasNoCoreExercises_ThrowsConflict_409()
    {
        var (db, user, clock, uow, sanitizer, _, _) = CreateHarness();
        await using (db)
        {
            const string theory = "{\"objectives\":[\"Test\"],\"sections\":[{\"heading\":\"H\",\"explanationVi\":\"E\"}]}";
            // Create A1-01 with NO core exercises
            var step = GrammarStepEntity.Create("A1-01", "A1", 1, "Bài 1", "Step 1", "Tóm tắt", theory);
            db.GrammarSteps.Add(step);
            await db.SaveChangesAsync();

            var handler = new StartGrammarAttemptCommandHandler(db, user, clock, uow, sanitizer);

            var ex = await Assert.ThrowsAsync<ConflictException>(() =>
                handler.HandleAsync(new StartGrammarAttemptCommand(GrammarAttemptKind.StepCore, "A1-01")));

            Assert.Equal("grammar_step_has_no_core_exercises", ex.Message);
        }
    }

    [Fact]
    public async Task StartAttempt_InProgressAttemptWithin24h_IsReused()
    {
        var (db, user, clock, uow, sanitizer, _, _) = CreateHarness();
        await using (db)
        {
            SeedRoadmapWithExercises(db);

            var handler = new StartGrammarAttemptCommandHandler(db, user, clock, uow, sanitizer);

            // First start creates attempt
            var attempt1 = await handler.HandleAsync(new StartGrammarAttemptCommand(GrammarAttemptKind.StepCore, "A1-01"));
            Assert.NotNull(attempt1);

            // Second start within 24h returns existing attempt
            var attempt2 = await handler.HandleAsync(new StartGrammarAttemptCommand(GrammarAttemptKind.StepCore, "A1-01"));
            Assert.Equal(attempt1.Id, attempt2.Id);

            var attemptsCount = await db.GrammarAttempts.CountAsync();
            Assert.Equal(1, attemptsCount);
        }
    }

    [Fact]
    public async Task StartAttempt_ForceNew_CreatesFreshAttempt_AndKeepsPreviousAnswers()
    {
        var (db, user, clock, uow, sanitizer, _, _) = CreateHarness();
        await using (db)
        {
            SeedRoadmapWithExercises(db);
            var handler = new StartGrammarAttemptCommandHandler(db, user, clock, uow, sanitizer);

            var first = await handler.HandleAsync(new StartGrammarAttemptCommand(GrammarAttemptKind.StepCore, "A1-01"));
            var firstEntity = await db.GrammarAttempts.Include(a => a.Answers).SingleAsync(a => a.Id == first.Id);
            firstEntity.SubmitAnswer(firstEntity.ExerciseIds[0], "{}", "Completed", 100m, 10m, 10m, null, "hash", clock.GetUtcNow());
            await db.SaveChangesAsync();

            var fresh = await handler.HandleAsync(new StartGrammarAttemptCommand(GrammarAttemptKind.StepCore, "A1-01", ForceNew: true));

            Assert.NotEqual(first.Id, fresh.Id);
            Assert.Empty(fresh.Answers);
            Assert.Equal(2, await db.GrammarAttempts.CountAsync());
            var kept = await db.GrammarAttempts.Include(a => a.Answers).AsNoTracking().SingleAsync(a => a.Id == first.Id);
            Assert.Single(kept.Answers);

            // Without ForceNew the newest unfinished attempt is resumed.
            var resumed = await handler.HandleAsync(new StartGrammarAttemptCommand(GrammarAttemptKind.StepCore, "A1-01"));
            Assert.Equal(fresh.Id, resumed.Id);
        }
    }

    [Fact]
    public async Task StartAttempt_LevelTest_LockedWhenPreviousLevelNotPassed_ThrowsForbidden_403()
    {
        var (db, user, clock, uow, sanitizer, _, _) = CreateHarness();
        await using (db)
        {
            SeedRoadmapWithExercises(db);

            var handler = new StartGrammarAttemptCommandHandler(db, user, clock, uow, sanitizer);

            // A2 level test locked because A1 is not passed
            var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
                handler.HandleAsync(new StartGrammarAttemptCommand(GrammarAttemptKind.LevelTest, null, "A2")));

            Assert.Equal("grammar_level_test_locked", ex.Message);
        }
    }

    [Fact]
    public async Task StartAttempt_LevelTest_Selects20To30ExercisesAcrossSteps()
    {
        var (db, user, clock, uow, sanitizer, _, _) = CreateHarness();
        await using (db)
        {
            SeedRoadmapWithExercises(db);

            var handler = new StartGrammarAttemptCommandHandler(db, user, clock, uow, sanitizer);

            // A1 level test is unlocked
            var attempt = await handler.HandleAsync(new StartGrammarAttemptCommand(GrammarAttemptKind.LevelTest, null, "A1"));

            Assert.NotNull(attempt);
            Assert.InRange(attempt.Exercises.Count, 20, 30);
            Assert.Equal("A1", attempt.CefrLevel);
            Assert.Equal(GrammarAttemptKind.LevelTest, attempt.Kind);
        }
    }

    [Fact]
    public async Task SubmitAnswer_ExerciseNotInAttempt_ThrowsConflict_409()
    {
        var (db, user, clock, uow, _, grader, aiGrader) = CreateHarness();
        await using (db)
        {
            SeedRoadmapWithExercises(db);

            var ex1 = await db.GrammarExercises.FirstAsync(e => e.Key == "A1-01-E01");
            var ex2 = await db.GrammarExercises.FirstAsync(e => e.Key == "A1-02-E01");

            var attempt = GrammarAttemptEntity.Start(user.UserId!, GrammarAttemptKind.Practice, ex1.StepId, "A1", [ex1.Id], DateTimeOffset.UtcNow);
            db.GrammarAttempts.Add(attempt);
            await db.SaveChangesAsync();

            var handler = new SubmitGrammarAnswerCommandHandler(db, user, clock, grader, aiGrader, uow, new ScoringRubricProvider(db));

            using var payloadDoc = JsonDocument.Parse("{\"selectedOptionIds\":[\"opt-1\"]}");

            await Assert.ThrowsAsync<ConflictException>(() =>
                handler.HandleAsync(new SubmitGrammarAnswerCommand(attempt.Id, ex2.Id, payloadDoc.RootElement)));
        }
    }

    [Fact]
    public async Task SubmitAnswer_DeterministicGrading_GradesCorrectly()
    {
        var (db, user, clock, uow, _, grader, aiGrader) = CreateHarness();
        await using (db)
        {
            SeedRoadmapWithExercises(db);

            var ex1 = await db.GrammarExercises.FirstAsync(e => e.Key == "A1-01-E01");

            var attempt = GrammarAttemptEntity.Start(user.UserId!, GrammarAttemptKind.Practice, ex1.StepId, "A1", [ex1.Id], DateTimeOffset.UtcNow);
            db.GrammarAttempts.Add(attempt);
            await db.SaveChangesAsync();

            var handler = new SubmitGrammarAnswerCommandHandler(db, user, clock, grader, aiGrader, uow, new ScoringRubricProvider(db));

            // Submit correct answer
            using var correctDoc = JsonDocument.Parse("{\"selectedOptionIds\":[\"opt-1\"]}");
            var correctRes = await handler.HandleAsync(new SubmitGrammarAnswerCommand(attempt.Id, ex1.Id, correctDoc.RootElement));

            Assert.Equal("Completed", correctRes.Status);
            Assert.Equal(100m, correctRes.Score100);
            Assert.Equal(10m, correctRes.PointsEarned);

            // Submit incorrect answer
            using var incorrectDoc = JsonDocument.Parse("{\"selectedOptionIds\":[\"opt-2\"]}");
            var incorrectRes = await handler.HandleAsync(new SubmitGrammarAnswerCommand(attempt.Id, ex1.Id, incorrectDoc.RootElement));

            Assert.Equal("Completed", incorrectRes.Status);
            Assert.Equal(0m, incorrectRes.Score100);
            Assert.Equal(0m, incorrectRes.PointsEarned);
        }
    }

    [Fact]
    public async Task SubmitAnswer_AiRubricWithoutKey_ReturnsSelfAssessedWithSuggestedAnswer()
    {
        var (db, user, clock, uow, _, grader, aiGrader) = CreateHarness();
        await using (db)
        {
            SeedRoadmapWithExercises(db);

            var step = await db.GrammarSteps.FirstAsync(s => s.Code == "A1-01");
            var transEx = GrammarExerciseEntity.CreateOfficial(
                step.Id, "A1-01-E99", ExerciseType.Translation, 2, "Translate", "Translate into English",
                "{\"prompt\":\"Anh ấy đi học mỗi ngày.\"}",
                "{\"referenceTranslations\":[\"He goes to school every day.\"],\"rubricCode\":\"LESSON_TRANSLATION\"}",
                10m, GradingMode.AiRubric, isCore: true, contentHash: ValidHash(999), now: DateTimeOffset.UtcNow);
            db.GrammarExercises.Add(transEx);

            var attempt = GrammarAttemptEntity.Start(user.UserId!, GrammarAttemptKind.StepCore, step.Id, "A1", [transEx.Id], DateTimeOffset.UtcNow);
            db.GrammarAttempts.Add(attempt);
            await db.SaveChangesAsync();

            var handler = new SubmitGrammarAnswerCommandHandler(db, user, clock, grader, aiGrader, uow, new ScoringRubricProvider(db));

            using var answerDoc = JsonDocument.Parse("{\"writtenText\":\"He goes to school every day.\"}");
            var res = await handler.HandleAsync(new SubmitGrammarAnswerCommand(attempt.Id, transEx.Id, answerDoc.RootElement, AiConfig: null));

            Assert.Equal("SelfAssessed", res.Status);
            Assert.Null(res.Score100);
            Assert.Equal("He goes to school every day.", res.SuggestedAnswer);
        }
    }

    [Fact]
    public async Task SubmitAnswer_AiRubricWithKey_InvokesAiGraderBeforeCommit()
    {
        var (db, user, clock, innerUow, _, grader, aiGrader) = CreateHarness();
        var countingUow = new CountingUnitOfWork(innerUow);

        await using (db)
        {
            SeedRubrics(db);
            SeedRoadmapWithExercises(db);

            var step = await db.GrammarSteps.FirstAsync(s => s.Code == "A1-01");
            var transEx = GrammarExerciseEntity.CreateOfficial(
                step.Id, "A1-01-E99", ExerciseType.Translation, 2, "Translate", "Translate into English",
                "{\"prompt\":\"Anh ấy đi học mỗi ngày.\"}",
                "{\"referenceTranslations\":[\"He goes to school every day.\"],\"rubricCode\":\"LESSON_TRANSLATION\"}",
                10m, GradingMode.AiRubric, isCore: true, contentHash: ValidHash(999), now: DateTimeOffset.UtcNow);
            db.GrammarExercises.Add(transEx);

            var attempt = GrammarAttemptEntity.Start(user.UserId!, GrammarAttemptKind.StepCore, step.Id, "A1", [transEx.Id], DateTimeOffset.UtcNow);
            db.GrammarAttempts.Add(attempt);
            await db.SaveChangesAsync();

            bool aiCalledBeforeCommit = false;
            aiGrader.OnGradeCalled = () =>
            {
                // Ensure no commit happened yet when AI provider was invoked
                aiCalledBeforeCommit = countingUow.CommitCount == 0;
            };

            var handler = new SubmitGrammarAnswerCommandHandler(db, user, clock, grader, aiGrader, countingUow, new ScoringRubricProvider(db));

            using var answerDoc = JsonDocument.Parse("{\"writtenText\":\"He goes to school every day.\"}");
            var res = await handler.HandleAsync(new SubmitGrammarAnswerCommand(
                attempt.Id, transEx.Id, answerDoc.RootElement,
                new LessonAiExecutionConfigDto(ApiKey: "dummy-byo-key")));

            Assert.True(aiCalledBeforeCommit);
            Assert.Equal(1, countingUow.CommitCount);
            Assert.Equal("Completed", res.Status);
            Assert.Equal(95m, res.Score100);
            Assert.Equal("He goes to school every day.", res.SuggestedAnswer);
        }
    }

    [Fact]
    public async Task SubmitAnswer_AiProviderThrows_RecordsFailedGrading_DoesNotThrow500()
    {
        var (db, user, clock, uow, _, grader, aiGrader) = CreateHarness();
        await using (db)
        {
            SeedRubrics(db);
            SeedRoadmapWithExercises(db);

            var step = await db.GrammarSteps.FirstAsync(s => s.Code == "A1-01");
            var transEx = GrammarExerciseEntity.CreateOfficial(
                step.Id, "A1-01-E99", ExerciseType.Translation, 2, "Translate", "Translate into English",
                "{\"prompt\":\"Anh ấy đi học mỗi ngày.\"}",
                "{\"referenceTranslations\":[\"He goes to school every day.\"],\"rubricCode\":\"LESSON_TRANSLATION\"}",
                10m, GradingMode.AiRubric, isCore: true, contentHash: ValidHash(999), now: DateTimeOffset.UtcNow);
            db.GrammarExercises.Add(transEx);

            var attempt = GrammarAttemptEntity.Start(user.UserId!, GrammarAttemptKind.StepCore, step.Id, "A1", [transEx.Id], DateTimeOffset.UtcNow);
            db.GrammarAttempts.Add(attempt);
            await db.SaveChangesAsync();

            aiGrader.Handler = () => throw new HttpRequestException("Gemini quota exceeded.");

            var handler = new SubmitGrammarAnswerCommandHandler(db, user, clock, grader, aiGrader, uow, new ScoringRubricProvider(db));

            using var answerDoc = JsonDocument.Parse("{\"writtenText\":\"He goes to school every day.\"}");
            var res = await handler.HandleAsync(new SubmitGrammarAnswerCommand(
                attempt.Id, transEx.Id, answerDoc.RootElement,
                new LessonAiExecutionConfigDto(ApiKey: "dummy-byo-key")));

            Assert.Equal("FailedGrading", res.Status);
            Assert.Null(res.Score100);
        }
    }

    [Fact]
    public async Task CompleteAttempt_Score69_DoesNotPass()
    {
        var (db, user, clock, uow, _, _, _) = CreateHarness();
        await using (db)
        {
            SeedRoadmapWithExercises(db);

            var ex1 = await db.GrammarExercises.FirstAsync(e => e.Key == "A1-01-E01");

            var attempt = GrammarAttemptEntity.Start(user.UserId!, GrammarAttemptKind.StepCore, ex1.StepId, "A1", [ex1.Id], DateTimeOffset.UtcNow);
            db.GrammarAttempts.Add(attempt);

            attempt.SubmitAnswer(ex1.Id, "{}", "Completed", 69m, 6.9m, 10m, null, ex1.ContentHash, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();

            var handler = new CompleteGrammarAttemptCommandHandler(db, user, clock, uow);
            var result = await handler.HandleAsync(new CompleteGrammarAttemptCommand(attempt.Id));

            Assert.Equal(69m, result.Score100);
            Assert.False(result.Passed);
            Assert.Empty(result.UnlockedStepCodes);

            var progress = await db.UserGrammarStepProgress.FirstAsync(p => p.UserId == user.UserId && p.StepId == ex1.StepId);
            Assert.Equal(GrammarStepProgressStatus.Unlocked, progress.Status);
            Assert.Equal(69m, progress.BestScore100);
        }
    }

    [Fact]
    public async Task CompleteAttempt_Score70_Passes_AndUnlocksNextStep()
    {
        var (db, user, clock, uow, _, _, _) = CreateHarness();
        await using (db)
        {
            SeedRoadmapWithExercises(db);

            var ex1 = await db.GrammarExercises.FirstAsync(e => e.Key == "A1-01-E01");

            var attempt = GrammarAttemptEntity.Start(user.UserId!, GrammarAttemptKind.StepCore, ex1.StepId, "A1", [ex1.Id], DateTimeOffset.UtcNow);
            db.GrammarAttempts.Add(attempt);

            attempt.SubmitAnswer(ex1.Id, "{}", "Completed", 70m, 7.0m, 10m, null, ex1.ContentHash, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();

            var handler = new CompleteGrammarAttemptCommandHandler(db, user, clock, uow);
            var result = await handler.HandleAsync(new CompleteGrammarAttemptCommand(attempt.Id));

            Assert.Equal(70m, result.Score100);
            Assert.True(result.Passed);
            Assert.Contains("A1-02", result.UnlockedStepCodes);

            var progress = await db.UserGrammarStepProgress.FirstAsync(p => p.UserId == user.UserId && p.StepId == ex1.StepId);
            Assert.Equal(GrammarStepProgressStatus.Passed, progress.Status);
            Assert.Equal(70m, progress.BestScore100);
            Assert.Equal(GrammarStepPassedBy.Steps, progress.PassedBy);
        }
    }

    [Fact]
    public async Task CompleteAttempt_LevelTestScore80_MarksAllLevelStepsPassed()
    {
        var (db, user, clock, uow, _, _, _) = CreateHarness();
        await using (db)
        {
            SeedRoadmapWithExercises(db);

            var allA1Exercises = await db.GrammarExercises
                .Where(e => e.Key!.StartsWith("A1-"))
                .Take(20)
                .ToListAsync();

            var attempt = GrammarAttemptEntity.Start(user.UserId!, GrammarAttemptKind.LevelTest, null, "A1", allA1Exercises.Select(e => e.Id).ToList(), DateTimeOffset.UtcNow);
            db.GrammarAttempts.Add(attempt);

            foreach (var ex in allA1Exercises)
            {
                attempt.SubmitAnswer(ex.Id, "{}", "Completed", 85m, 8.5m, 10m, null, ex.ContentHash, DateTimeOffset.UtcNow);
            }
            await db.SaveChangesAsync();

            var handler = new CompleteGrammarAttemptCommandHandler(db, user, clock, uow);
            var result = await handler.HandleAsync(new CompleteGrammarAttemptCommand(attempt.Id));

            Assert.Equal(85m, result.Score100);
            Assert.True(result.Passed);
            Assert.True(result.LevelPassed);
            Assert.Contains("A2-01", result.UnlockedStepCodes);

            // All 20 steps of A1 must now be Passed by LevelTest
            var a1StepIds = await db.GrammarSteps.Where(s => s.CefrLevel == "A1").Select(s => s.Id).ToListAsync();
            var progresses = await db.UserGrammarStepProgress.Where(p => p.UserId == user.UserId && a1StepIds.Contains(p.StepId)).ToListAsync();

            Assert.Equal(20, progresses.Count);
            Assert.All(progresses, p =>
            {
                Assert.Equal(GrammarStepProgressStatus.Passed, p.Status);
                Assert.Equal(GrammarStepPassedBy.LevelTest, p.PassedBy);
            });
        }
    }

    [Fact]
    public async Task CompleteAttempt_PracticeAttempt_DoesNotAlterProgress()
    {
        var (db, user, clock, uow, _, _, _) = CreateHarness();
        await using (db)
        {
            SeedRoadmapWithExercises(db);

            var ex1 = await db.GrammarExercises.FirstAsync(e => e.Key == "A1-01-E01");

            var attempt = GrammarAttemptEntity.Start(user.UserId!, GrammarAttemptKind.Practice, ex1.StepId, "A1", [ex1.Id], DateTimeOffset.UtcNow);
            db.GrammarAttempts.Add(attempt);

            attempt.SubmitAnswer(ex1.Id, "{}", "Completed", 100m, 10m, 10m, null, ex1.ContentHash, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();

            var handler = new CompleteGrammarAttemptCommandHandler(db, user, clock, uow);
            var result = await handler.HandleAsync(new CompleteGrammarAttemptCommand(attempt.Id));

            Assert.Equal(100m, result.Score100);
            Assert.False(result.Passed);

            // No progress record created or altered
            var progress = await db.UserGrammarStepProgress.FirstOrDefaultAsync(p => p.UserId == user.UserId && p.StepId == ex1.StepId);
            Assert.Null(progress);
        }
    }
}
