using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Features.Lessons.Commands.AttemptOperations;
using EnglishLiveTutor.Application.Features.Lessons.Commands.DraftOperations;
using EnglishLiveTutor.Application.Features.Lessons.Commands.ExerciseOperations;
using EnglishLiveTutor.Application.Features.Lessons.Commands.ImportOperations;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Lessons.Queries.AttemptQueries;
using EnglishLiveTutor.Application.Features.Lessons.Queries.GetLessonById;
using EnglishLiveTutor.Application.Features.Lessons.Queries.ListLessons;
using EnglishLiveTutor.Infrastructure.Lessons;
using EnglishLiveTutor.Domain.LessonAttempts.Enums;
using EnglishLiveTutor.Domain.Lessons.Enums;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class LessonCqrsIntegrationTests
{
    private static (AppDbContext Db, FakeTimeProvider Clock, FakeCurrentUser User) CreateTestContext(string dbName)
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var user = new FakeCurrentUser { UserId = "teacher-author-1" };
        var interceptor = new AuditingSaveChangesInterceptor(clock, user);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(interceptor)
            .Options;

        var db = new AppDbContext(options);
        return (db, clock, user);
    }

    [Fact]
    public async Task LessonDraft_FullLifecycle_ShouldCreate_AddExercises_AndPublish()
    {
        string dbName = Guid.NewGuid().ToString("N");
        var (db, clock, user) = CreateTestContext(dbName);

        var sanitizer = new LessonSanitizer();
        var getByIdHandler = new GetLessonByIdQueryHandler(db, sanitizer);
        var draftHandlers = new DraftCommandHandlers(db, user, clock, getByIdHandler);
        var exerciseHandlers = new ExerciseCommandHandlers(db, getByIdHandler);

        // 1. Create Draft Lesson
        var createReq = new CreateLessonDraftRequest(
            Title: "Dining at a French Bistro",
            CefrLevel: "B1",
            Topic: "Food and Dining",
            Content: "Essential vocabulary and ordering phrases.",
            EstimatedMinutes: 30,
            LearningObjectives: new List<string> { "Order polite requests", "Ask about allergens" },
            TargetVocabulary: new List<string> { "appetizer", "specialty" },
            TargetGrammar: new List<string> { "Could I have..." });

        var createdDraft = await draftHandlers.HandleAsync(new CreateLessonDraftCommand(createReq));

        Assert.NotNull(createdDraft);
        Assert.Equal("Dining at a French Bistro", createdDraft.Title);
        Assert.Equal("Draft", createdDraft.Status);
        Assert.Equal(1, createdDraft.Version);
        Assert.Empty(createdDraft.Exercises);

        // 2. Add Exercises to Draft
        var ex1Req = new AddExerciseRequest(
            Key: "ex-01-vocab",
            Type: "Flashcards",
            Title: "Bistro Vocabulary",
            Instructions: "Review vocabulary",
            MaxPoints: 0,
            GradingMode: "SelfAssessment",
            Content: JsonNode.Parse("{\"cards\":[{\"id\":\"c1\",\"term\":\"croissant\"}]}")!);

        await exerciseHandlers.HandleAsync(new AddExerciseToDraftCommand(createdDraft.Id, ex1Req));

        var ex2Req = new AddExerciseRequest(
            Key: "ex-02-mcq",
            Type: "MultipleChoice",
            Title: "Ordering Quiz",
            Instructions: "Pick the best polite request",
            MaxPoints: 5.0m,
            GradingMode: "Deterministic",
            Content: JsonNode.Parse("{\"question\":\"How to order?\",\"options\":[{\"id\":\"opt-1\",\"text\":\"Give me soup\"},{\"id\":\"opt-2\",\"text\":\"Could I please have soup?\"}]}")!,
            Grading: JsonNode.Parse("{\"correctOptionIds\":[\"opt-2\"],\"explanation\":\"Polite modal request\"}")!);

        var lessonWithExercises = await exerciseHandlers.HandleAsync(new AddExerciseToDraftCommand(createdDraft.Id, ex2Req));

        Assert.Equal(2, lessonWithExercises.Exercises.Count);
        Assert.Equal("ex-01-vocab", lessonWithExercises.Exercises[0].Key);
        Assert.Equal("ex-02-mcq", lessonWithExercises.Exercises[1].Key);

        // 3. Learner should NOT be able to view Draft lesson
        var learnerViewBeforePublish = await getByIdHandler.HandleAsync(new GetLessonByIdQuery(createdDraft.Id, IsLearner: true));
        Assert.Null(learnerViewBeforePublish);

        // 4. Publish Draft Lesson
        var publishResult = await draftHandlers.HandleAsync(new PublishLessonCommand(createdDraft.Id, new PublishLessonRequest(null)));

        Assert.Equal("Published", publishResult.Status);
        Assert.Equal(1, publishResult.Version);
        Assert.Equal(2, publishResult.ExerciseCount);
        Assert.Equal(5.0m, publishResult.TotalMaxPoints);

        // 5. Learner can now view Published lesson, with sanitized learner payload (no answer keys!)
        var learnerViewAfterPublish = await getByIdHandler.HandleAsync(new GetLessonByIdQuery(createdDraft.Id, IsLearner: true));
        Assert.NotNull(learnerViewAfterPublish);

        var learnerDto = Assert.IsType<LessonDetailLearnerDto>(learnerViewAfterPublish);
        Assert.Equal(2, learnerDto.Exercises.Count);

        var learnerMcq = learnerDto.Exercises[1];
        string contentJson = learnerMcq.Content?.ToJsonString() ?? "";
        Assert.DoesNotContain("correctOptionIds", contentJson); // correctOptionIds not in content!
        Assert.DoesNotContain("Polite modal request", contentJson); // explanation stripped!
    }

    [Fact]
    public async Task LessonAttempt_FullLifecycle_ShouldTrackRevision_GradeAnswers_AndComplete()
    {
        string dbName = Guid.NewGuid().ToString("N");
        var (db, clock, user) = CreateTestContext(dbName);

        // Seed Rubrics
        await LessonRubricSeeder.SeedAsync(db);

        var sanitizer = new LessonSanitizer();
        var dictationScorer = new DictationScorer();
        var deterministicGrader = new DeterministicExerciseGrader(dictationScorer);
        var aiGrader = new FakeLessonAiGrader();
        var audioRepo = new FakeAudioRecordingRepository();
        var mediaStorage = new FakeLessonMediaStorage();
        var unitOfWork = new UnitOfWork(db);
        var idempotency = new IdempotencyService(db);
        var resumeHandler = new AttemptQueryHandlers(db, user, sanitizer);

        var getByIdHandler = new GetLessonByIdQueryHandler(db, sanitizer);
        var draftHandlers = new DraftCommandHandlers(db, user, clock, getByIdHandler);
        var exerciseHandlers = new ExerciseCommandHandlers(db, getByIdHandler);
        var startAttemptHandler = new StartLessonAttemptCommandHandler(db, user, unitOfWork, idempotency, resumeHandler);
        var submitAnswerHandler = new SubmitExerciseAnswerCommandHandler(db, user, clock, deterministicGrader, aiGrader, audioRepo, mediaStorage, unitOfWork, idempotency);
        var completeAttemptHandler = new CompleteLessonAttemptCommandHandler(db, user, clock, unitOfWork, idempotency, resumeHandler);

        // 1. Create and Publish Lesson
        var draft = await draftHandlers.HandleAsync(new CreateLessonDraftCommand(new CreateLessonDraftRequest(
            Title: "Restaurant Dialogues",
            CefrLevel: "B1",
            Topic: "Dining",
            Content: "Desc",
            EstimatedMinutes: 20,
            null, null, null)));

        await exerciseHandlers.HandleAsync(new AddExerciseToDraftCommand(draft.Id, new AddExerciseRequest(
            Key: "ex-1-mcq",
            Type: "MultipleChoice",
            Title: "Quiz",
            Instructions: "Select best option",
            MaxPoints: 5.0m,
            GradingMode: "Deterministic",
            Content: JsonNode.Parse("{\"options\":[{\"id\":\"opt-a\"},{\"id\":\"opt-b\"}]}")!,
            Grading: JsonNode.Parse("{\"correctOptionIds\":[\"opt-b\"]}")!)));

        await exerciseHandlers.HandleAsync(new AddExerciseToDraftCommand(draft.Id, new AddExerciseRequest(
            Key: "ex-2-dictation",
            Type: "Dictation",
            Title: "Dictation Task",
            Instructions: "Type what you hear",
            MaxPoints: 5.0m,
            GradingMode: "Deterministic",
            Content: JsonNode.Parse("{\"audioAssetId\":\"audio-sample-1\"}")!,
            Grading: JsonNode.Parse("{\"transcript\":\"Our chef prepares fresh handmade pasta with imported olive oil\"}")!)));

        await draftHandlers.HandleAsync(new PublishLessonCommand(draft.Id, new PublishLessonRequest(null)));

        // 2. Start Attempt
        user.UserId = "learner-student-99";
        var attemptResume = await startAttemptHandler.HandleAsync(new StartLessonAttemptCommand(draft.Id));

        Assert.NotNull(attemptResume);
        Assert.Equal(1, attemptResume.AttemptRevision);
        Assert.Equal("InProgress", attemptResume.Status);
        Assert.Equal(10.0m, attemptResume.Progress.TotalMaxPoints);
        Assert.Equal(0m, attemptResume.Progress.CurrentSubtotalPoints);

        var ex1Id = attemptResume.Exercises[0].Id;
        var ex2Id = attemptResume.Exercises[1].Id;

        // 3. Submit Answer for Exercise 1 (MCQ) with revision 1 -> returns revision 2
        var ans1Req = new SubmitExerciseAnswerRequest(
            ExerciseId: ex1Id,
            SubQuestionId: null,
            AttemptRevision: 1,
            AnswerPayload: JsonNode.Parse("{\"selectedOptionIds\":[\"opt-b\"]}")!);

        var ans1Result = await submitAnswerHandler.HandleAsync(new SubmitExerciseAnswerCommand(attemptResume.AttemptId, ans1Req));

        Assert.Equal(2, ans1Result.AttemptRevision); // Revision incremented!
        Assert.Equal("Completed", ans1Result.Status);
        Assert.Equal(100m, ans1Result.Score100);
        Assert.Equal(5.0m, ans1Result.PointsEarned);
        Assert.Equal(5.0m, ans1Result.AttemptProgress.CurrentSubtotalPoints);
        Assert.Equal(1, ans1Result.AttemptProgress.CompletedExercisesCount);

        // 4. Concurrency test: Submitting again with stale revision 1 must throw InvalidOperationException
        var staleSubmitReq = new SubmitExerciseAnswerRequest(
            ExerciseId: ex2Id,
            SubQuestionId: null,
            AttemptRevision: 1, // Stale! Current is 2
            AnswerPayload: JsonNode.Parse("{\"transcribedText\":\"...\"}")!);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            submitAnswerHandler.HandleAsync(new SubmitExerciseAnswerCommand(attemptResume.AttemptId, staleSubmitReq)));

        // 5. Submit Answer for Exercise 2 (Dictation) with current revision 2
        // Reference has 10 words. Learner has 4 word edits (from spec worked example: accuracy 60%, 3.00 points)
        var ans2Req = new SubmitExerciseAnswerRequest(
            ExerciseId: ex2Id,
            SubQuestionId: null,
            AttemptRevision: 2,
            AnswerPayload: JsonNode.Parse("{\"transcribedText\":\"Our chef prepare fresh hand made pasta with imported oil\"}")!);

        var ans2Result = await submitAnswerHandler.HandleAsync(new SubmitExerciseAnswerCommand(attemptResume.AttemptId, ans2Req));

        Assert.Equal(3, ans2Result.AttemptRevision);
        Assert.Equal("Completed", ans2Result.Status);
        Assert.Equal(60.00m, ans2Result.Score100);
        Assert.Equal(3.00m, ans2Result.PointsEarned);
        Assert.Equal(8.00m, ans2Result.AttemptProgress.CurrentSubtotalPoints); // 5.00 + 3.00 = 8.00
        Assert.Equal(2, ans2Result.AttemptProgress.CompletedExercisesCount);

        // 6. Resume Attempt: Verify attemptRevision is 3 and safe snapshot returned
        var resumed = await resumeHandler.HandleAsync(new ResumeLessonAttemptQuery(attemptResume.AttemptId));
        Assert.NotNull(resumed);
        Assert.Equal(3, resumed.AttemptRevision);
        Assert.Equal(8.00m, resumed.Progress.CurrentSubtotalPoints);

        // 7. Complete Attempt: Final score calculation
        var completed = await completeAttemptHandler.HandleAsync(new CompleteLessonAttemptCommand(attemptResume.AttemptId));
        Assert.Equal("Completed", completed.Status);
        // Subtotal = 8.00 out of 10.00 -> 80.00%
        Assert.Equal(80.00m, completed.Progress.FinalScore100);
    }

    [Fact]
    public async Task ImportPreview_WithValidJson_ShouldReturnValidCandidate()
    {
        string dbName = Guid.NewGuid().ToString("N");
        var (db, clock, user) = CreateTestContext(dbName);

        var validator = new EnglishLiveTutor.Application.Features.Lessons.Validation.LessonCandidateValidator(db);
        var previewHandler = new PreviewLessonImportCommandHandler(validator);

        string validEnvelope = @"{
            ""schemaVersion"": ""lesson-exercises.v1"",
            ""lesson"": {
                ""title"": ""Hotel Check-in"",
                ""cefrLevel"": ""A2"",
                ""topic"": ""Hospitality"",
                ""estimatedMinutes"": 15
            },
            ""exercises"": [
                {
                    ""key"": ""ex-hotel-vocab"",
                    ""type"": ""Flashcards"",
                    ""title"": ""Hotel Words"",
                    ""content"": { ""cards"": [] }
                }
            ]
        }";

        var preview = await previewHandler.HandleAsync(new PreviewLessonImportCommand(validEnvelope));

        Assert.True(preview.IsValid);
        Assert.Empty(preview.Issues);
        Assert.NotNull(preview.Candidate);
    }

    [Fact]
    public async Task ListLessons_DraftVisibility_DependsOnIsLearner()
    {
        string dbName = Guid.NewGuid().ToString("N");
        var (db, clock, user) = CreateTestContext(dbName);

        var sanitizer = new LessonSanitizer();
        var getByIdHandler = new GetLessonByIdQueryHandler(db, sanitizer);
        var draftHandlers = new DraftCommandHandlers(db, user, clock, getByIdHandler);
        var exerciseHandlers = new ExerciseCommandHandlers(db, getByIdHandler);
        var listHandler = new ListLessonsQueryHandler(db);

        // Create Draft 1
        var draft1 = await draftHandlers.HandleAsync(new CreateLessonDraftCommand(new CreateLessonDraftRequest(
            Title: "Draft Lesson One",
            CefrLevel: "A1",
            Topic: "Greetings",
            Content: "Intro",
            EstimatedMinutes: 10,
            null, null, null)));

        // Create Draft 2 and publish it
        var draft2 = await draftHandlers.HandleAsync(new CreateLessonDraftCommand(new CreateLessonDraftRequest(
            Title: "Published Lesson Two",
            CefrLevel: "A2",
            Topic: "Travel",
            Content: "Airport checkin",
            EstimatedMinutes: 15,
            null, null, null)));
        await exerciseHandlers.HandleAsync(new AddExerciseToDraftCommand(draft2.Id, new AddExerciseRequest(
            Key: "ex-vocab",
            Type: "Flashcards",
            Title: "Vocab",
            Instructions: "Learn words",
            MaxPoints: 0,
            GradingMode: "SelfAssessment",
            Content: JsonNode.Parse("{\"cards\":[]}")!)));
        await draftHandlers.HandleAsync(new PublishLessonCommand(draft2.Id, new PublishLessonRequest(null)));

        // When IsLearner = true -> only published lesson returned (Count = 1)
        var learnerResult = await listHandler.HandleAsync(new ListLessonsQuery(IsLearner: true));
        Assert.Single(learnerResult.Items);
        Assert.Equal(draft2.Id, learnerResult.Items[0].Id);
        Assert.Equal("Published", learnerResult.Items[0].Status);

        // When IsLearner = false (Admin/Author view) -> both Draft and Published lessons returned (Count = 2)
        var authorResult = await listHandler.HandleAsync(new ListLessonsQuery(IsLearner: false));
        Assert.Equal(2, authorResult.Items.Count);
        Assert.Contains(authorResult.Items, l => l.Id == draft1.Id && l.Status == "Draft");
        Assert.Contains(authorResult.Items, l => l.Id == draft2.Id && l.Status == "Published");
    }

    [Fact]
    public async Task BuildLessonPromptCommand_WhenFilteringByShadowing_ShouldOnlyIncludeShadowingRulesAndExamples()
    {
        string dbName = Guid.NewGuid().ToString("N");
        var (db, _, _) = CreateTestContext(dbName);
        var templateProvider = new EnglishLiveTutor.Infrastructure.Lessons.PromptBuilder.LessonPromptTemplateProvider();
        var promptBuilder = new EnglishLiveTutor.Infrastructure.Lessons.PromptBuilder.LessonPromptBuilder(templateProvider);
        var promptHandler = new BuildLessonPromptCommandHandler(promptBuilder);

        var promptCmd = new BuildLessonPromptCommand(
            Topic: "Daily Routines",
            CefrLevel: "A2",
            LessonTitle: "Morning Routines",
            PreferredExerciseTypes: new List<string> { "Shadowing" },
            TargetExerciseCount: 5,
            CustomInstructions: null);

        LessonPromptResultDto result = await promptHandler.HandleAsync(promptCmd);
        string userPrompt = result.UserPrompt;

        // Verify Shadowing is included
        Assert.Contains("Preferred Exercise Types: Shadowing", userPrompt);
        Assert.Contains("Shadowing", userPrompt);
        Assert.Contains("\"type\": \"Shadowing\"", userPrompt);
        Assert.Contains("LESSON_SHADOWING", userPrompt);

        // Verify other unrequested exercise types are NOT present in the critical rules or examples
        Assert.DoesNotContain("SentenceUnscramble", userPrompt);
        Assert.DoesNotContain("MultipleChoice", userPrompt);
        Assert.DoesNotContain("FillInTheBlanks", userPrompt);
        Assert.DoesNotContain("Matching", userPrompt);
    }
}
