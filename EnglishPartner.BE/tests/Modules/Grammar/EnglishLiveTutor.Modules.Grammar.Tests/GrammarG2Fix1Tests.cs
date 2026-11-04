using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.Grammar;
using EnglishLiveTutor.Application.Features.Grammar.Commands;
using EnglishLiveTutor.Application.Features.Grammar.Commands.Admin;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Application.Features.Grammar.Queries;
using EnglishLiveTutor.Application.Features.Grammar.Rules;
using EnglishLiveTutor.Application.Features.Grammar.Services;
using EnglishLiveTutor.Application.Features.Identity.Services;
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

public class GrammarG2Fix1Tests
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

    private sealed class CapturingAiGrader : ILessonAiGrader
    {
        public string? LastTaskContextJson { get; private set; }
        public Func<Task<LessonAiGradingOutcome>>? Handler { get; set; }

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
            LastTaskContextJson = taskContextJson;

            if (Handler != null)
            {
                return await Handler();
            }

            return new LessonAiGradingOutcome(
                IsAssessable: true,
                Score100: 90m,
                PointsEarned: Math.Round(90m / 100m * maxPoints, 2),
                CriteriaBreakdown: [new LessonAiCriterionScore("ACCURACY", "Accuracy", 1m, 4, "Great", "Detailed note")],
                Strengths: ["Well structured"],
                Improvements: ["Better word choice"],
                SuggestedAnswer: "Suggested answer from AI."
            );
        }
    }

    private sealed class FailingUnitOfWork : IUnitOfWork
    {
        private readonly IUnitOfWork _inner;
        private readonly Exception? _exceptionToThrowOnCommit;
        private readonly int _failOnAttempt;
        private int _attempts = 0;

        public FailingUnitOfWork(IUnitOfWork inner, Exception? exceptionToThrowOnCommit, int failOnAttempt = 1)
        {
            _inner = inner;
            _exceptionToThrowOnCommit = exceptionToThrowOnCommit;
            _failOnAttempt = failOnAttempt;
        }

        public async Task<int> CommitChangesAsync(CancellationToken ct = default)
        {
            _attempts++;
            if (_attempts == _failOnAttempt && _exceptionToThrowOnCommit != null)
            {
                throw _exceptionToThrowOnCommit;
            }
            return await _inner.CommitChangesAsync(ct);
        }

        public Task<TResponse> ExecuteInTransactionAsync<TResponse>(Func<CancellationToken, Task<TResponse>> operation, CancellationToken ct = default) =>
            _inner.ExecuteInTransactionAsync(operation, ct);
    }

    private static string ValidHash(int index = 1) => index.ToString("x").PadLeft(64, '0');

    private static (AppDbContext Db, FakeCurrentUser User, TimeProvider Clock, IUnitOfWork Uow, IGrammarLearnerContentSanitizer Sanitizer, IDeterministicExerciseGrader Grader, CapturingAiGrader AiGrader) CreateHarness()
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
        var aiGrader = new CapturingAiGrader();

        return (db, user, clock, uow, sanitizer, grader, aiGrader);
    }

    private static void SeedRubrics(AppDbContext db)
    {
        var transRubric = new ScoringRubricEntity("LESSON_TRANSLATION", "Translation Rubric", "Translation", 1);
        transRubric.AddCriterion("ACCURACY", "Accuracy", "Accuracy", 1.0m, displayOrder: 1);
        db.ScoringRubrics.Add(transRubric);

        var writingRubric = new ScoringRubricEntity("LESSON_WRITING", "Writing Rubric", "Writing", 1);
        writingRubric.AddCriterion("GRAMMAR", "Grammar", "Grammar", 1.0m, displayOrder: 1);
        db.ScoringRubrics.Add(writingRubric);

        db.SaveChanges();
    }

    private static (GrammarStepEntity Step, GrammarExerciseEntity McExercise) SeedStepWithMcExercise(AppDbContext db, string stepCode = "A1-01", bool isCore = true)
    {
        var step = GrammarStepEntity.Create(stepCode, "A1", 1, "Bài 1", "Step 1", "Tóm tắt", "{}");
        db.GrammarSteps.Add(step);

        var ex = GrammarExerciseEntity.CreateOfficial(
            stepId: step.Id,
            key: $"{stepCode}-E01",
            type: ExerciseType.MultipleChoice,
            difficulty: 1,
            title: "MC Test",
            instructions: "Select one",
            contentJson: "{\"question\":\"Choose\",\"options\":[{\"id\":\"o1\",\"text\":\"Option 1\"},{\"id\":\"o2\",\"text\":\"Option 2\"}]}",
            gradingDefinitionJson: "{\"correctOptionIds\":[\"o1\"],\"explanation\":\"Correct\"}",
            maxPoints: 10m,
            gradingMode: GradingMode.Deterministic,
            isCore: isCore,
            contentHash: ValidHash(1),
            now: DateTimeOffset.UtcNow);

        db.GrammarExercises.Add(ex);
        db.SaveChanges();
        return (step, ex);
    }

    // =========================================================================
    // Item 1: Khóa câu trả lời trong attempt StepCore và LevelTest
    // =========================================================================

    [Fact]
    public async Task Item1_SubmitAnswer_StepCore_SecondSubmit_ThrowsConflict_AnswerLocked()
    {
        var (db, user, clock, uow, _, grader, aiGrader) = CreateHarness();
        await using (db)
        {
            var (step, ex) = SeedStepWithMcExercise(db);
            var attempt = GrammarAttemptEntity.Start(user.UserId!, GrammarAttemptKind.StepCore, step.Id, "A1", [ex.Id], DateTimeOffset.UtcNow);
            db.GrammarAttempts.Add(attempt);
            await db.SaveChangesAsync();

            var handler = new SubmitGrammarAnswerCommandHandler(db, user, clock, grader, aiGrader, uow, new ScoringRubricProvider(db));
            var payload = JsonDocument.Parse("{\"selectedOptionIds\":[\"o1\"]}").RootElement;

            // First submission -> OK
            var firstResult = await handler.HandleAsync(new SubmitGrammarAnswerCommand(attempt.Id, ex.Id, payload));
            Assert.Equal("Completed", firstResult.Status);

            // Second submission -> 409 ConflictException "grammar_answer_locked"
            var conflict = await Assert.ThrowsAsync<ConflictException>(() =>
                handler.HandleAsync(new SubmitGrammarAnswerCommand(attempt.Id, ex.Id, payload)));

            Assert.Equal("grammar_answer_locked", conflict.Message);
        }
    }

    [Fact]
    public async Task Item1_SubmitAnswer_Practice_AllowsMultipleSubmissions()
    {
        var (db, user, clock, uow, _, grader, aiGrader) = CreateHarness();
        await using (db)
        {
            var (step, ex) = SeedStepWithMcExercise(db);
            var attempt = GrammarAttemptEntity.Start(user.UserId!, GrammarAttemptKind.Practice, step.Id, "A1", [ex.Id], DateTimeOffset.UtcNow);
            db.GrammarAttempts.Add(attempt);
            await db.SaveChangesAsync();

            var handler = new SubmitGrammarAnswerCommandHandler(db, user, clock, grader, aiGrader, uow, new ScoringRubricProvider(db));
            var wrongPayload = JsonDocument.Parse("{\"selectedOptionIds\":[\"o2\"]}").RootElement;
            var correctPayload = JsonDocument.Parse("{\"selectedOptionIds\":[\"o1\"]}").RootElement;

            // First submission -> OK (wrong answer)
            var res1 = await handler.HandleAsync(new SubmitGrammarAnswerCommand(attempt.Id, ex.Id, wrongPayload));
            Assert.Equal(0m, res1.Score100);

            // Second submission -> OK (correct answer, resubmission permitted in Practice)
            var res2 = await handler.HandleAsync(new SubmitGrammarAnswerCommand(attempt.Id, ex.Id, correctPayload));
            Assert.Equal(100m, res2.Score100);
        }
    }

    [Fact]
    public async Task Item1_SubmitAnswer_StepCore_FailedGrading_CanResubmit()
    {
        var (db, user, clock, uow, _, grader, aiGrader) = CreateHarness();
        await using (db)
        {
            SeedRubrics(db);
            var step = GrammarStepEntity.Create("A1-01", "A1", 1, "Bài 1", "Step 1", "Tóm tắt", "{}");
            db.GrammarSteps.Add(step);

            var writingEx = GrammarExerciseEntity.CreateOfficial(
                stepId: step.Id,
                key: "A1-01-W01",
                type: ExerciseType.WritingPractice,
                difficulty: 1,
                title: "Writing",
                instructions: "Write a sentence",
                contentJson: "{\"prompt\":\"Introduce yourself\",\"minWords\":3,\"maxWords\":10}",
                gradingDefinitionJson: "{\"guidelines\":\"Be clear\",\"rubricCode\":\"LESSON_WRITING\",\"rubricVersion\":1}",
                maxPoints: 10m,
                gradingMode: GradingMode.AiRubric,
                isCore: true,
                contentHash: ValidHash(2),
                now: DateTimeOffset.UtcNow);

            db.GrammarExercises.Add(writingEx);
            var attempt = GrammarAttemptEntity.Start(user.UserId!, GrammarAttemptKind.StepCore, step.Id, "A1", [writingEx.Id], DateTimeOffset.UtcNow);
            db.GrammarAttempts.Add(attempt);
            await db.SaveChangesAsync();

            var handler = new SubmitGrammarAnswerCommandHandler(db, user, clock, grader, aiGrader, uow, new ScoringRubricProvider(db));
            var payload = JsonDocument.Parse("{\"text\":\"I am a student.\"}").RootElement;
            var aiConfig = new LessonAiExecutionConfigDto("secret-api-key", "gemini-1.5-flash");

            // First submit: AI grader throws -> status is FailedGrading
            aiGrader.Handler = () => throw new HttpRequestException("Gemini provider network error");
            var res1 = await handler.HandleAsync(new SubmitGrammarAnswerCommand(attempt.Id, writingEx.Id, payload, aiConfig));
            Assert.Equal("FailedGrading", res1.Status);

            // Second submit: AI grader succeeds -> resubmission succeeds because existing status is FailedGrading
            aiGrader.Handler = null;
            var res2 = await handler.HandleAsync(new SubmitGrammarAnswerCommand(attempt.Id, writingEx.Id, payload, aiConfig));
            Assert.Equal("Completed", res2.Status);
            Assert.Equal(90m, res2.Score100);
        }
    }

    // =========================================================================
    // Item 2: Đáp án mẫu AiRubric
    // =========================================================================

    [Fact]
    public async Task Item2_AiRubric_SuggestedAnswer_NeverReadFromContentJson_OnlyGradingDefinition()
    {
        var (db, user, clock, uow, _, grader, aiGrader) = CreateHarness();
        await using (db)
        {
            var step = GrammarStepEntity.Create("A1-01", "A1", 1, "Bài 1", "Step 1", "Tóm tắt", "{}");
            db.GrammarSteps.Add(step);

            // Exercise with decoy suggestedAnswer in ContentJson and real suggestedAnswer in GradingDefinitionJson
            var writingEx = GrammarExerciseEntity.CreateOfficial(
                stepId: step.Id,
                key: "A1-01-W02",
                type: ExerciseType.WritingPractice,
                difficulty: 1,
                title: "Writing",
                instructions: "Write a sentence",
                contentJson: "{\"prompt\":\"Introduce yourself\",\"suggestedAnswer\":\"DECOY_IN_CONTENT\"}",
                gradingDefinitionJson: "{\"suggestedAnswer\":\"REAL_FROM_GRADING\"}",
                maxPoints: 10m,
                gradingMode: GradingMode.AiRubric,
                isCore: true,
                contentHash: ValidHash(3),
                now: DateTimeOffset.UtcNow);

            db.GrammarExercises.Add(writingEx);
            var attempt = GrammarAttemptEntity.Start(user.UserId!, GrammarAttemptKind.StepCore, step.Id, "A1", [writingEx.Id], DateTimeOffset.UtcNow);
            db.GrammarAttempts.Add(attempt);
            await db.SaveChangesAsync();

            var handler = new SubmitGrammarAnswerCommandHandler(db, user, clock, grader, aiGrader, uow, new ScoringRubricProvider(db));
            var payload = JsonDocument.Parse("{\"text\":\"Hello.\"}").RootElement;

            // Submit without BYO key -> SelfAssessed
            var res = await handler.HandleAsync(new SubmitGrammarAnswerCommand(attempt.Id, writingEx.Id, payload));
            Assert.Equal("SelfAssessed", res.Status);
            Assert.Equal("REAL_FROM_GRADING", res.SuggestedAnswer);
            Assert.DoesNotContain("DECOY_IN_CONTENT", res.SuggestedAnswer);
        }
    }

    [Fact]
    public async Task Item2_AiRubric_FailedGrading_DoesNotExposeSuggestedAnswer()
    {
        var (db, user, clock, uow, _, grader, aiGrader) = CreateHarness();
        await using (db)
        {
            SeedRubrics(db);
            var (step, ex) = SeedStepWithMcExercise(db);

            var writingEx = GrammarExerciseEntity.CreateOfficial(
                stepId: step.Id,
                key: "A1-01-W03",
                type: ExerciseType.WritingPractice,
                difficulty: 1,
                title: "Writing",
                instructions: "Write",
                contentJson: "{\"prompt\":\"Prompt\"}",
                gradingDefinitionJson: "{\"rubricCode\":\"LESSON_WRITING\",\"suggestedAnswer\":\"Secret suggested answer\"}",
                maxPoints: 10m,
                gradingMode: GradingMode.AiRubric,
                isCore: false,
                contentHash: ValidHash(4),
                now: DateTimeOffset.UtcNow);

            db.GrammarExercises.Add(writingEx);
            var attempt = GrammarAttemptEntity.Start(user.UserId!, GrammarAttemptKind.StepCore, step.Id, "A1", [writingEx.Id], DateTimeOffset.UtcNow);
            db.GrammarAttempts.Add(attempt);
            await db.SaveChangesAsync();

            var handler = new SubmitGrammarAnswerCommandHandler(db, user, clock, grader, aiGrader, uow, new ScoringRubricProvider(db));
            var payload = JsonDocument.Parse("{\"text\":\"Submission\"}").RootElement;
            var aiConfig = new LessonAiExecutionConfigDto("api-key", "gemini-1.5-flash");

            aiGrader.Handler = () => throw new Exception("AI failure");
            var res = await handler.HandleAsync(new SubmitGrammarAnswerCommand(attempt.Id, writingEx.Id, payload, aiConfig));

            Assert.Equal("FailedGrading", res.Status);
            Assert.Null(res.SuggestedAnswer);
            Assert.False(res.Feedback?.TryGetProperty("suggestedAnswer", out _));
        }
    }

    // =========================================================================
    // Item 3: Task context cho AI grader
    // =========================================================================

    [Fact]
    public async Task Item3_TaskContext_BuiltCorrectly_ForTranslationAndWritingPractice()
    {
        var (db, user, clock, uow, _, grader, aiGrader) = CreateHarness();
        await using (db)
        {
            SeedRubrics(db);
            var step = GrammarStepEntity.Create("A1-01", "A1", 1, "Bài 1", "Step 1", "Tóm tắt", "{}");
            db.GrammarSteps.Add(step);

            var transEx = GrammarExerciseEntity.CreateOfficial(
                stepId: step.Id,
                key: "A1-01-T01",
                type: ExerciseType.Translation,
                difficulty: 1,
                title: "Translation",
                instructions: "Translate",
                contentJson: "{\"sourceText\":\"Xin chao\",\"direction\":\"vi_to_en\",\"context\":\"Greeting\"}",
                gradingDefinitionJson: "{\"referenceTranslations\":[\"Hello\",\"Hi\"],\"requiredKeyIdeas\":[\"greeting\"],\"rubricCode\":\"LESSON_TRANSLATION\"}",
                maxPoints: 10m,
                gradingMode: GradingMode.AiRubric,
                isCore: false,
                contentHash: ValidHash(5),
                now: DateTimeOffset.UtcNow);

            var writeEx = GrammarExerciseEntity.CreateOfficial(
                stepId: step.Id,
                key: "A1-01-W04",
                type: ExerciseType.WritingPractice,
                difficulty: 1,
                title: "Writing",
                instructions: "Write",
                contentJson: "{\"prompt\":\"Describe your pet\",\"minWords\":5,\"maxWords\":20}",
                gradingDefinitionJson: "{\"guidelines\":\"Use adjectives\",\"rubricCode\":\"LESSON_WRITING\"}",
                maxPoints: 10m,
                gradingMode: GradingMode.AiRubric,
                isCore: false,
                contentHash: ValidHash(6),
                now: DateTimeOffset.UtcNow);

            db.GrammarExercises.AddRange(transEx, writeEx);
            var attempt = GrammarAttemptEntity.Start(user.UserId!, GrammarAttemptKind.StepCore, step.Id, "A1", [transEx.Id, writeEx.Id], DateTimeOffset.UtcNow);
            db.GrammarAttempts.Add(attempt);
            await db.SaveChangesAsync();

            var handler = new SubmitGrammarAnswerCommandHandler(db, user, clock, grader, aiGrader, uow, new ScoringRubricProvider(db));
            var aiConfig = new LessonAiExecutionConfigDto("api-key", "gemini-1.5-flash");

            // 1. Submit Translation
            var transPayload = JsonDocument.Parse("{\"translation\":\"Hello\"}").RootElement;
            await handler.HandleAsync(new SubmitGrammarAnswerCommand(attempt.Id, transEx.Id, transPayload, aiConfig));
            Assert.NotNull(aiGrader.LastTaskContextJson);
            var transContextDoc = JsonDocument.Parse(aiGrader.LastTaskContextJson);
            Assert.Equal("Xin chao", transContextDoc.RootElement.GetProperty("sourceText").GetString());
            Assert.Equal("vi_to_en", transContextDoc.RootElement.GetProperty("direction").GetString());
            Assert.Equal("Greeting", transContextDoc.RootElement.GetProperty("context").GetString());
            Assert.True(transContextDoc.RootElement.TryGetProperty("referenceTranslations", out _));

            // 2. Submit WritingPractice
            var writePayload = JsonDocument.Parse("{\"text\":\"I have a cute dog.\" }").RootElement;
            await handler.HandleAsync(new SubmitGrammarAnswerCommand(attempt.Id, writeEx.Id, writePayload, aiConfig));
            Assert.NotNull(aiGrader.LastTaskContextJson);
            var writeContextDoc = JsonDocument.Parse(aiGrader.LastTaskContextJson);
            Assert.Equal("Describe your pet", writeContextDoc.RootElement.GetProperty("prompt").GetString());
            Assert.Equal(5, writeContextDoc.RootElement.GetProperty("minWords").GetInt32());
            Assert.Equal(20, writeContextDoc.RootElement.GetProperty("maxWords").GetInt32());
            Assert.Equal("Use adjectives", writeContextDoc.RootElement.GetProperty("guidelines").GetString());
        }
    }

    // =========================================================================
    // Item 4: Không để lộ đáp án qua content & Learner Content Sanitizer
    // =========================================================================

    [Fact]
    public void Item4_GrammarExerciseRules_RejectsForbiddenAnswerKeysInContent()
    {
        // 1. blanks[].acceptedAnswers in FillInTheBlanks
        const string fitbWithAccepted = "{\"textWithBlanks\":\"She [blank-1] a doctor.\",\"blanks\":[{\"id\":\"blank-1\",\"acceptedAnswers\":[\"is\"]}]}";
        const string fitbGrading = "{\"blankAnswers\":{\"blank-1\":[\"is\"]}}";
        var issues1 = GrammarExerciseRules.Validate(ExerciseType.FillInTheBlanks, fitbWithAccepted, fitbGrading, 10m, GradingMode.Deterministic);
        Assert.Contains(issues1, i => i.Severity == "Error" && i.Message.Contains("'acceptedAnswers'"));

        // 2. correctOptionIds in MultipleChoice content
        const string mcWithAnswer = "{\"question\":\"Q\",\"options\":[{\"id\":\"o1\",\"text\":\"A\"}],\"correctOptionIds\":[\"o1\"]}";
        const string mcGrading = "{\"correctOptionIds\":[\"o1\"]}";
        var issues2 = GrammarExerciseRules.Validate(ExerciseType.MultipleChoice, mcWithAnswer, mcGrading, 10m, GradingMode.Deterministic);
        Assert.Contains(issues2, i => i.Severity == "Error" && i.Message.Contains("'correctOptionIds'"));

        // 3. suggestedAnswer in WritingPractice content
        const string writingWithAnswer = "{\"prompt\":\"P\",\"suggestedAnswer\":\"Forbidden answer\"}";
        var issues3 = GrammarExerciseRules.Validate(ExerciseType.WritingPractice, writingWithAnswer, "{}", 10m, GradingMode.AiRubric);
        Assert.Contains(issues3, i => i.Severity == "Error" && i.Message.Contains("'suggestedAnswer'"));

        // 4. ReadingComprehension subQuestions[].grading is EXEMPTED from forbidden check
        const string rcWithSubGrading = """
        {
          "passage": "Passage text...",
          "subQuestions": [
            {
              "id": "q1",
              "questionKind": "SingleChoice",
              "maxPoints": 5,
              "options": [{"id":"o1","text":"Opt 1"}],
              "grading": {"correctOptionIds":["o1"]}
            }
          ]
        }
        """;
        var issues4 = GrammarExerciseRules.Validate(ExerciseType.ReadingComprehension, rcWithSubGrading, "{}", 5m, GradingMode.Composite);
        Assert.DoesNotContain(issues4, i => i.Message.Contains("forbidden key"));
    }

    [Fact]
    public void Item4_GrammarLearnerContentSanitizer_AllNineTypes_OnlyKeepAllowlist()
    {
        var sanitizer = new GrammarLearnerContentSanitizer();

        // 1. MultipleChoice
        var mcDirty = "{\"question\":\"Q\",\"selectionMode\":\"Single\",\"options\":[{\"id\":\"o1\",\"text\":\"A\",\"isCorrect\":true}],\"correctOptionIds\":[\"o1\"],\"explanation\":\"exp\"}";
        var mcClean = JsonDocument.Parse(sanitizer.Sanitize(ExerciseType.MultipleChoice, mcDirty)).RootElement;
        Assert.True(mcClean.TryGetProperty("question", out _));
        Assert.True(mcClean.TryGetProperty("selectionMode", out _));
        Assert.False(mcClean.TryGetProperty("correctOptionIds", out _));
        Assert.False(mcClean.TryGetProperty("explanation", out _));
        Assert.False(mcClean.GetProperty("options")[0].TryGetProperty("isCorrect", out _));

        // 2. TrueFalse
        var tfDirty = "{\"context\":\"C\",\"statements\":[{\"id\":\"s1\",\"text\":\"T\",\"answer\":true}],\"statementAnswers\":{\"s1\":true}}";
        var tfClean = JsonDocument.Parse(sanitizer.Sanitize(ExerciseType.TrueFalse, tfDirty)).RootElement;
        Assert.True(tfClean.TryGetProperty("context", out _));
        Assert.False(tfClean.TryGetProperty("statementAnswers", out _));
        Assert.False(tfClean.GetProperty("statements")[0].TryGetProperty("answer", out _));

        // 3. Matching
        var mDirty = "{\"leftItems\":[{\"id\":\"l1\",\"text\":\"L\"}],\"rightItems\":[{\"id\":\"r1\",\"text\":\"R\"}],\"correctPairs\":[{\"leftId\":\"l1\",\"rightId\":\"r1\"}]}";
        var mClean = JsonDocument.Parse(sanitizer.Sanitize(ExerciseType.Matching, mDirty)).RootElement;
        Assert.True(mClean.TryGetProperty("leftItems", out _));
        Assert.True(mClean.TryGetProperty("rightItems", out _));
        Assert.False(mClean.TryGetProperty("correctPairs", out _));

        // 4. FillInTheBlanks
        var fitbDirty = "{\"textWithBlanks\":\"Text [b1]\",\"blanks\":[{\"id\":\"b1\",\"hint\":\"H\",\"acceptedAnswers\":[\"ans\"]}],\"answers\":[\"ans\"]}";
        var fitbClean = JsonDocument.Parse(sanitizer.Sanitize(ExerciseType.FillInTheBlanks, fitbDirty)).RootElement;
        Assert.True(fitbClean.TryGetProperty("textWithBlanks", out _));
        Assert.True(fitbClean.GetProperty("blanks")[0].TryGetProperty("hint", out _));
        Assert.False(fitbClean.GetProperty("blanks")[0].TryGetProperty("acceptedAnswers", out _));
        Assert.False(fitbClean.TryGetProperty("answers", out _));

        // 5. SentenceUnscramble
        var suDirty = "{\"prompt\":\"P\",\"tokens\":[{\"id\":\"t1\",\"text\":\"T\"}],\"validSequences\":[[\"t1\"]]}";
        var suClean = JsonDocument.Parse(sanitizer.Sanitize(ExerciseType.SentenceUnscramble, suDirty)).RootElement;
        Assert.True(suClean.TryGetProperty("prompt", out _));
        Assert.True(suClean.TryGetProperty("tokens", out _));
        Assert.False(suClean.TryGetProperty("validSequences", out _));

        // 6. ErrorCorrection
        var ecDirty = "{\"incorrectSentence\":\"I is here.\",\"instructions\":\"Fix it\",\"referenceCorrections\":[\"I am here.\"]}";
        var ecClean = JsonDocument.Parse(sanitizer.Sanitize(ExerciseType.ErrorCorrection, ecDirty)).RootElement;
        Assert.True(ecClean.TryGetProperty("incorrectSentence", out _));
        Assert.False(ecClean.TryGetProperty("referenceCorrections", out _));

        // 7. Translation
        var trDirty = "{\"direction\":\"vi_to_en\",\"sourceText\":\"Xin chao\",\"context\":\"C\",\"referenceTranslations\":[\"Hello\"],\"suggestedAnswer\":\"Hello\"}";
        var trClean = JsonDocument.Parse(sanitizer.Sanitize(ExerciseType.Translation, trDirty)).RootElement;
        Assert.True(trClean.TryGetProperty("sourceText", out _));
        Assert.False(trClean.TryGetProperty("referenceTranslations", out _));
        Assert.False(trClean.TryGetProperty("suggestedAnswer", out _));

        // 8. WritingPractice
        var wpDirty = "{\"prompt\":\"Write\",\"minWords\":5,\"maxWords\":10,\"suggestedAnswer\":\"Sample answer\"}";
        var wpClean = JsonDocument.Parse(sanitizer.Sanitize(ExerciseType.WritingPractice, wpDirty)).RootElement;
        Assert.True(wpClean.TryGetProperty("prompt", out _));
        Assert.False(wpClean.TryGetProperty("suggestedAnswer", out _));

        // 9. ReadingComprehension
        var rcDirty = """
        {
          "passage": "Passage...",
          "subQuestions": [
            {
              "id": "q1",
              "questionKind": "SingleChoice",
              "order": 1,
              "maxPoints": 5,
              "questionText": "Question 1",
              "options": [{"id":"o1","text":"Opt"}],
              "grading": {"correctOptionIds":["o1"]}
            }
          ]
        }
        """;
        var rcClean = JsonDocument.Parse(sanitizer.Sanitize(ExerciseType.ReadingComprehension, rcDirty)).RootElement;
        Assert.True(rcClean.TryGetProperty("passage", out _));
        Assert.True(rcClean.GetProperty("subQuestions")[0].TryGetProperty("questionText", out _));
        Assert.False(rcClean.GetProperty("subQuestions")[0].TryGetProperty("grading", out _));
    }

    // =========================================================================
    // Item 5: CompleteGrammarAttemptCommand: Xóa mềm bỏ khỏi mẫu số; tái dùng attempt
    // =========================================================================

    [Fact]
    public async Task Item5_CompleteAttempt_SoftDeletedExercise_ExcludedFromDenominator()
    {
        var (db, user, clock, uow, _, _, _) = CreateHarness();
        await using (db)
        {
            var step = GrammarStepEntity.Create("A1-01", "A1", 1, "Bài 1", "Step 1", "Tóm tắt", "{}");
            db.GrammarSteps.Add(step);

            var ex1 = GrammarExerciseEntity.CreateOfficial(
                step.Id, "A1-01-E01", ExerciseType.MultipleChoice, 1, "E1", "I1", "{}", "{}", 10m, GradingMode.Deterministic, true, ValidHash(1), DateTimeOffset.UtcNow);
            var ex2 = GrammarExerciseEntity.CreateOfficial(
                step.Id, "A1-01-E02", ExerciseType.MultipleChoice, 1, "E2", "I2", "{}", "{}", 10m, GradingMode.Deterministic, true, ValidHash(2), DateTimeOffset.UtcNow);

            db.GrammarExercises.AddRange(ex1, ex2);

            // Attempt was started when both exercises were active
            var attempt = GrammarAttemptEntity.Start(user.UserId!, GrammarAttemptKind.StepCore, step.Id, "A1", [ex1.Id, ex2.Id], DateTimeOffset.UtcNow);
            db.GrammarAttempts.Add(attempt);

            // Learner answers ex1 with 10/10 points
            attempt.SubmitAnswer(ex1.Id, "{}", "Completed", 100m, 10m, 10m, null, ex1.ContentHash, DateTimeOffset.UtcNow);

            // Now ex2 is soft-deleted before attempt completion
            ex2.SoftDelete(DateTimeOffset.UtcNow);

            await db.SaveChangesAsync();

            var handler = new CompleteGrammarAttemptCommandHandler(db, user, clock, uow);
            var result = await handler.HandleAsync(new CompleteGrammarAttemptCommand(attempt.Id));

            // Denominator must be 10 (ex1 only), not 20 (ex1 + ex2). Score is 100%, passed!
            Assert.Equal(100m, result.Score100);
            Assert.True(result.Passed);
        }
    }

    [Fact]
    public async Task Item5_StartAttempt_DoesNotReuseAttempt_IfAnyExerciseIsDeleted()
    {
        var (db, user, clock, uow, sanitizer, _, _) = CreateHarness();
        await using (db)
        {
            var step = GrammarStepEntity.Create("A1-01", "A1", 1, "Bài 1", "Step 1", "Tóm tắt", "{}");
            db.GrammarSteps.Add(step);

            var ex1 = GrammarExerciseEntity.CreateOfficial(
                step.Id, "A1-01-E01", ExerciseType.MultipleChoice, 1, "E1", "I1", "{}", "{}", 10m, GradingMode.Deterministic, true, ValidHash(1), DateTimeOffset.UtcNow);
            var ex2 = GrammarExerciseEntity.CreateOfficial(
                step.Id, "A1-01-E02", ExerciseType.MultipleChoice, 1, "E2", "I2", "{}", "{}", 10m, GradingMode.Deterministic, true, ValidHash(2), DateTimeOffset.UtcNow);

            db.GrammarExercises.AddRange(ex1, ex2);

            // Existing uncompleted attempt referencing ex1 and ex2
            var oldAttempt = GrammarAttemptEntity.Start(user.UserId!, GrammarAttemptKind.StepCore, step.Id, "A1", [ex1.Id, ex2.Id], DateTimeOffset.UtcNow);
            db.GrammarAttempts.Add(oldAttempt);

            // Soft-delete ex2
            ex2.SoftDelete(DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();

            var handler = new StartGrammarAttemptCommandHandler(db, user, clock, uow, sanitizer);
            var newAttemptDto = await handler.HandleAsync(new StartGrammarAttemptCommand(GrammarAttemptKind.StepCore, "A1-01"));

            // Must NOT reuse old attempt because ex2 was deleted
            Assert.NotEqual(oldAttempt.Id, newAttemptDto.Id);
        }
    }

    // =========================================================================
    // Item 6: Concurrency, Unique conflict & Race progress in LevelTest
    // =========================================================================

    [Fact]
    public async Task Item6_SubmitAnswer_CompletedAttempt_ThrowsConflict_409()
    {
        var (db, user, clock, uow, _, grader, aiGrader) = CreateHarness();
        await using (db)
        {
            var (step, ex) = SeedStepWithMcExercise(db);
            var attempt = GrammarAttemptEntity.Start(user.UserId!, GrammarAttemptKind.StepCore, step.Id, "A1", [ex.Id], DateTimeOffset.UtcNow);
            attempt.Complete(100m, DateTimeOffset.UtcNow);
            db.GrammarAttempts.Add(attempt);
            await db.SaveChangesAsync();

            var handler = new SubmitGrammarAnswerCommandHandler(db, user, clock, grader, aiGrader, uow, new ScoringRubricProvider(db));
            var payload = JsonDocument.Parse("{\"selectedOptionIds\":[\"o1\"]}").RootElement;

            var conflict = await Assert.ThrowsAsync<ConflictException>(() =>
                handler.HandleAsync(new SubmitGrammarAnswerCommand(attempt.Id, ex.Id, payload)));

            Assert.Contains("completed", conflict.Message);
        }
    }

    [Fact]
    public async Task Item6_SubmitAnswer_ConcurrencyTokenException_ThrowsConflict_409()
    {
        var (db, user, clock, innerUow, _, grader, aiGrader) = CreateHarness();
        await using (db)
        {
            var (step, ex) = SeedStepWithMcExercise(db);
            var attempt = GrammarAttemptEntity.Start(user.UserId!, GrammarAttemptKind.StepCore, step.Id, "A1", [ex.Id], DateTimeOffset.UtcNow);
            db.GrammarAttempts.Add(attempt);
            await db.SaveChangesAsync();

            var failingUow = new FailingUnitOfWork(innerUow, new DbUpdateConcurrencyException("Concurrency conflict"));
            var handler = new SubmitGrammarAnswerCommandHandler(db, user, clock, grader, aiGrader, failingUow, new ScoringRubricProvider(db));
            var payload = JsonDocument.Parse("{\"selectedOptionIds\":[\"o1\"]}").RootElement;

            var conflict = await Assert.ThrowsAsync<ConflictException>(() =>
                handler.HandleAsync(new SubmitGrammarAnswerCommand(attempt.Id, ex.Id, payload)));

            Assert.Equal("grammar_conflict", conflict.Message);
        }
    }

    [Fact]
    public async Task Item6_CompleteAttempt_LevelTest_ProgressRace_MarksPassedWithoutThrowing()
    {
        var (db, user, clock, innerUow, _, _, _) = CreateHarness();
        await using (db)
        {
            var step1 = GrammarStepEntity.Create("A1-01", "A1", 1, "B1", "S1", "T", "{}");
            db.GrammarSteps.Add(step1);
            var ex1 = GrammarExerciseEntity.CreateOfficial(step1.Id, "A1-01-E01", ExerciseType.MultipleChoice, 1, "E1", "I", "{}", "{}", 10m, GradingMode.Deterministic, true, ValidHash(1), DateTimeOffset.UtcNow);
            db.GrammarExercises.Add(ex1);

            var attempt = GrammarAttemptEntity.Start(user.UserId!, GrammarAttemptKind.LevelTest, null, "A1", [ex1.Id], DateTimeOffset.UtcNow);
            db.GrammarAttempts.Add(attempt);
            attempt.SubmitAnswer(ex1.Id, "{}", "Completed", 100m, 10m, 10m, null, ex1.ContentHash, DateTimeOffset.UtcNow);

            // Progress already exists (simulating race condition where another process created it)
            var existingProgress = UserGrammarStepProgressEntity.Create(user.UserId!, step1.Id, DateTimeOffset.UtcNow);
            db.UserGrammarStepProgress.Add(existingProgress);
            await db.SaveChangesAsync();

            // FailingUnitOfWork throws unique constraint violation on the first attempt, then succeeds on retry
            var raceEx = new DbUpdateException("Race unique constraint", new ArgumentException("An item with the same key has already been added."));
            var failingUow = new FailingUnitOfWork(innerUow, raceEx, failOnAttempt: 1);

            var handler = new CompleteGrammarAttemptCommandHandler(db, user, clock, failingUow);

            // Must NOT throw; catches unique conflict, reloads progresses, marks them MarkPassedByLevelTest and succeeds
            var result = await handler.HandleAsync(new CompleteGrammarAttemptCommand(attempt.Id));

            Assert.True(result.Passed);
            Assert.True(result.LevelPassed);

            // Progress is marked passed by LevelTest
            var updatedProgress = await db.UserGrammarStepProgress.FirstAsync(p => p.UserId == user.UserId && p.StepId == step1.Id);
            Assert.Equal(GrammarStepProgressStatus.Passed, updatedProgress.Status);
            Assert.Equal(GrammarStepPassedBy.LevelTest, updatedProgress.PassedBy);
        }
    }

    // =========================================================================
    // Item 7: Bộ core phải có ≥ 1 bài Deterministic/Composite
    // =========================================================================

    [Fact]
    public async Task Item7_StartAttempt_StepCore_OnlyAiRubric_ThrowsConflict_409()
    {
        var (db, user, clock, uow, sanitizer, _, _) = CreateHarness();
        await using (db)
        {
            var step = GrammarStepEntity.Create("A1-01", "A1", 1, "Bài 1", "Step 1", "Tóm tắt", "{}");
            db.GrammarSteps.Add(step);

            // Step only has AiRubric core exercise
            var writingEx = GrammarExerciseEntity.CreateOfficial(
                step.Id, "A1-01-W01", ExerciseType.WritingPractice, 1, "W", "I", "{}", "{}", 10m, GradingMode.AiRubric, true, ValidHash(1), DateTimeOffset.UtcNow);
            db.GrammarExercises.Add(writingEx);
            await db.SaveChangesAsync();

            var handler = new StartGrammarAttemptCommandHandler(db, user, clock, uow, sanitizer);

            var conflict = await Assert.ThrowsAsync<ConflictException>(() =>
                handler.HandleAsync(new StartGrammarAttemptCommand(GrammarAttemptKind.StepCore, "A1-01")));

            Assert.Equal("grammar_step_has_no_core_exercises", conflict.Message);
        }
    }

    [Fact]
    public async Task Item7_CompleteAttempt_GradedCountZero_DoesNotPass()
    {
        var (db, user, clock, uow, _, _, _) = CreateHarness();
        await using (db)
        {
            var (step, ex) = SeedStepWithMcExercise(db);
            var attempt = GrammarAttemptEntity.Start(user.UserId!, GrammarAttemptKind.StepCore, step.Id, "A1", [ex.Id], DateTimeOffset.UtcNow);
            db.GrammarAttempts.Add(attempt);

            // Answer is FailedGrading (gradedCount == 0)
            attempt.SubmitAnswer(ex.Id, "{}", "FailedGrading", null, null, 10m, null, ex.ContentHash, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();

            var handler = new CompleteGrammarAttemptCommandHandler(db, user, clock, uow);
            var result = await handler.HandleAsync(new CompleteGrammarAttemptCommand(attempt.Id));

            Assert.False(result.Passed);
            Assert.Equal(0m, result.Score100);
        }
    }

    // =========================================================================
    // Item 8: GetGrammarAttemptQuery và StartGrammarAttempt: IsVisibleTo(currentUser)
    // =========================================================================

    [Fact]
    public async Task Item8_GetGrammarAttemptQuery_UnavailableExercise_ReturnsPlaceholder()
    {
        var (db, user, clock, _, sanitizer, _, _) = CreateHarness();
        await using (db)
        {
            var step = GrammarStepEntity.Create("A1-01", "A1", 1, "Bài 1", "Step 1", "Tóm tắt", "{}");
            db.GrammarSteps.Add(step);

            var ex1 = GrammarExerciseEntity.CreateOfficial(step.Id, "A1-01-E01", ExerciseType.MultipleChoice, 1, "E1", "I", "{\"question\":\"Q1\"}", "{}", 10m, GradingMode.Deterministic, true, ValidHash(1), DateTimeOffset.UtcNow);
            var ex2 = GrammarExerciseEntity.CreateOfficial(step.Id, "A1-01-E02", ExerciseType.MultipleChoice, 1, "E2", "I", "{\"question\":\"Q2\"}", "{}", 10m, GradingMode.Deterministic, true, ValidHash(2), DateTimeOffset.UtcNow);

            db.GrammarExercises.AddRange(ex1, ex2);
            var attempt = GrammarAttemptEntity.Start(user.UserId!, GrammarAttemptKind.StepCore, step.Id, "A1", [ex1.Id, ex2.Id], DateTimeOffset.UtcNow);
            db.GrammarAttempts.Add(attempt);

            // ex2 is deleted
            ex2.SoftDelete(DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();

            var handler = new GetGrammarAttemptQueryHandler(db, user, sanitizer);
            var result = await handler.HandleAsync(new GetGrammarAttemptQuery(attempt.Id));

            Assert.Equal(2, result.Exercises.Count);
            var ex1Dto = result.Exercises.First(e => e.Id == ex1.Id);
            Assert.False(ex1Dto.Unavailable);
            Assert.NotNull(ex1Dto.Content);

            var ex2Dto = result.Exercises.First(e => e.Id == ex2.Id);
            Assert.True(ex2Dto.Unavailable);
            Assert.Null(ex2Dto.Content);
        }
    }

    // =========================================================================
    // Item 9: Kiểm trùng hash khi admin tạo/sửa bài Official
    // =========================================================================

    [Fact]
    public async Task Item9_AdminCreateOfficial_IgnoresPrivateOrPendingHash_ChecksOnlyOfficialAndApproved()
    {
        var (db, _, clock, uow, _, _, _) = CreateHarness();
        await using (db)
        {
            var step = GrammarStepEntity.Create("A1-01", "A1", 1, "Bài 1", "Step 1", "Tóm tắt", "{}");
            db.GrammarSteps.Add(step);

            const string mcContent = "{\"question\":\"Choose\",\"options\":[{\"id\":\"o1\",\"text\":\"A\"},{\"id\":\"o2\",\"text\":\"B\"}],\"selectionMode\":\"Single\"}";
            const string mcGrading = "{\"correctOptionIds\":[\"o1\"]}";
            string computedHash = GrammarContentHasher.ComputeHash(ExerciseType.MultipleChoice, mcContent, mcGrading);

            // Another learner has a Private/Pending exercise with this exact hash
            var communityPendingEx = GrammarExerciseEntity.CreateUserOwned(
                stepId: step.Id,
                ownerUserId: Guid.NewGuid().ToString(),
                source: GrammarExerciseSource.UserAi,
                visibility: GrammarExerciseVisibility.Public,
                type: ExerciseType.MultipleChoice,
                difficulty: 1,
                title: "User Pending",
                instructions: "Choose",
                contentJson: mcContent,
                gradingDefinitionJson: mcGrading,
                maxPoints: 10m,
                gradingMode: GradingMode.Deterministic,
                contentHash: computedHash,
                now: DateTimeOffset.UtcNow);

            db.GrammarExercises.Add(communityPendingEx);
            await db.SaveChangesAsync();

            var handler = new CreateOfficialGrammarExerciseCommandHandler(db, clock, uow);
            var contentElem = JsonDocument.Parse(mcContent).RootElement;
            var gradingElem = JsonDocument.Parse(mcGrading).RootElement;

            // Admin creating Official exercise with identical hash as Pending community exercise -> SUCCEEDS!
            var officialEx = await handler.HandleAsync(new CreateOfficialGrammarExerciseCommand(
                Code: "A1-01",
                Key: "A1-01-E01",
                Type: ExerciseType.MultipleChoice,
                Difficulty: 1,
                Title: "Admin Official",
                Instructions: "Choose",
                MaxPoints: 10m,
                GradingMode: GradingMode.Deterministic,
                Content: contentElem,
                Grading: gradingElem,
                IsCore: true));

            Assert.NotNull(officialEx);
            Assert.Equal(computedHash, officialEx.ContentHash);

            // Now, attempting to create a second Official exercise with that same hash -> THROWS CONFLICT!
            var conflict = await Assert.ThrowsAsync<ConflictException>(() =>
                handler.HandleAsync(new CreateOfficialGrammarExerciseCommand(
                    Code: "A1-01",
                    Key: "A1-01-E02",
                    Type: ExerciseType.MultipleChoice,
                    Difficulty: 1,
                    Title: "Duplicate Official",
                    Instructions: "Choose",
                    MaxPoints: 10m,
                    GradingMode: GradingMode.Deterministic,
                    Content: contentElem,
                    Grading: gradingElem,
                    IsCore: true)));

            Assert.Contains("identical content hash", conflict.Message);
        }
    }

    // =========================================================================
    // Item 10: Lỗi provider & AnswerPayload length
    // =========================================================================

    [Fact]
    public async Task Item10_SubmitAnswer_AiProviderThrows_FeedbackContainsFixedCodeAndExceptionType_NoExMessage()
    {
        var (db, user, clock, uow, _, grader, aiGrader) = CreateHarness();
        await using (db)
        {
            SeedRubrics(db);
            var step = GrammarStepEntity.Create("A1-01", "A1", 1, "Bài 1", "Step 1", "Tóm tắt", "{}");
            db.GrammarSteps.Add(step);

            var writingEx = GrammarExerciseEntity.CreateOfficial(
                step.Id, "A1-01-W01", ExerciseType.WritingPractice, 1, "W", "I", "{\"prompt\":\"P\"}", "{\"rubricCode\":\"LESSON_WRITING\"}", 10m, GradingMode.AiRubric, true, ValidHash(1), DateTimeOffset.UtcNow);
            db.GrammarExercises.Add(writingEx);

            var attempt = GrammarAttemptEntity.Start(user.UserId!, GrammarAttemptKind.StepCore, step.Id, "A1", [writingEx.Id], DateTimeOffset.UtcNow);
            db.GrammarAttempts.Add(attempt);
            await db.SaveChangesAsync();

            // Provider throws an exception containing sensitive leak info
            aiGrader.Handler = () => throw new InvalidOperationException("LEAK_SENSITIVE_KEY_SECRET");

            var handler = new SubmitGrammarAnswerCommandHandler(db, user, clock, grader, aiGrader, uow, new ScoringRubricProvider(db));
            var payload = JsonDocument.Parse("{\"text\":\"Sample\"}").RootElement;
            var aiConfig = new LessonAiExecutionConfigDto("key", "gemini");

            var res = await handler.HandleAsync(new SubmitGrammarAnswerCommand(attempt.Id, writingEx.Id, payload, aiConfig));

            Assert.Equal("FailedGrading", res.Status);
            Assert.NotNull(res.Feedback);
            var fb = res.Feedback.Value;
            Assert.Equal("ai_grading_failed", fb.GetProperty("error").GetString());
            Assert.Equal("InvalidOperationException", fb.GetProperty("exceptionType").GetString());

            // Must NOT contain ex.Message ("LEAK_SENSITIVE_KEY_SECRET")
            Assert.DoesNotContain("LEAK_SENSITIVE_KEY_SECRET", fb.GetRawText());
        }
    }

    [Fact]
    public async Task Item10_SubmitAnswer_PayloadExceeds8000Chars_ThrowsArgumentException_400()
    {
        var (db, user, clock, uow, _, grader, aiGrader) = CreateHarness();
        await using (db)
        {
            var (step, ex) = SeedStepWithMcExercise(db);
            var attempt = GrammarAttemptEntity.Start(user.UserId!, GrammarAttemptKind.StepCore, step.Id, "A1", [ex.Id], DateTimeOffset.UtcNow);
            db.GrammarAttempts.Add(attempt);
            await db.SaveChangesAsync();

            var handler = new SubmitGrammarAnswerCommandHandler(db, user, clock, grader, aiGrader, uow, new ScoringRubricProvider(db));
            string hugeText = new string('x', 8050);
            var payload = JsonDocument.Parse($"{{\"huge\":\"{hugeText}\"}}").RootElement;

            var exArg = await Assert.ThrowsAsync<ArgumentException>(() =>
                handler.HandleAsync(new SubmitGrammarAnswerCommand(attempt.Id, ex.Id, payload)));

            Assert.Contains("8,000", exArg.Message);
        }
    }

    // =========================================================================
    // Item 11: ListGrammarExercisesQuery: Luôn phân trang
    // =========================================================================

    [Fact]
    public async Task Item11_ListGrammarExercises_AlwaysPaged_Default20_Max100()
    {
        var (db, user, clock, _, sanitizer, _, _) = CreateHarness();
        await using (db)
        {
            var step = GrammarStepEntity.Create("A1-01", "A1", 1, "Bài 1", "Step 1", "Tóm tắt", "{}");
            db.GrammarSteps.Add(step);

            // Add 25 exercises to step
            for (int i = 1; i <= 25; i++)
            {
                db.GrammarExercises.Add(GrammarExerciseEntity.CreateOfficial(
                    step.Id, $"A1-01-E{i:D2}", ExerciseType.MultipleChoice, 1, $"E{i}", "I", "{\"question\":\"Q\"}", "{\"correctOptionIds\":[\"o1\"]}", 10m, GradingMode.Deterministic, true, ValidHash(i), DateTimeOffset.UtcNow));
            }
            await db.SaveChangesAsync();

            var handler = new ListGrammarExercisesQueryHandler(db, new UserDirectory(db), user, sanitizer);

            // 1. Without page or pageSize -> defaults to PageSize = 20
            var defaultResult = await handler.HandleAsync(new ListGrammarExercisesQuery("A1-01"));
            Assert.Equal(20, defaultResult.PageSize);
            Assert.Equal(20, defaultResult.Items.Count);
            Assert.Equal(25, defaultResult.TotalCount);
            Assert.Equal(2, defaultResult.TotalPages);

            // 2. With PageSize = 250 -> clamped to 100
            var clampedResult = await handler.HandleAsync(new ListGrammarExercisesQuery("A1-01", Page: 1, PageSize: 250));
            Assert.Equal(100, clampedResult.PageSize);
            Assert.Equal(25, clampedResult.Items.Count);
        }
    }
}
