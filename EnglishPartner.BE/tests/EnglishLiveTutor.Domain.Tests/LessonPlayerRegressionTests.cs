using System.Text.Json;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Lessons.Commands.SynthesizeExerciseAudio;
using EnglishLiveTutor.Application.Features.Lessons.Commands.AttemptOperations;
using EnglishLiveTutor.Application.Features.Lessons.Queries.AttemptQueries;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Domain.LessonAttempts;
using EnglishLiveTutor.Domain.Lessons;
using EnglishLiveTutor.Domain.Lessons.Enums;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;
using EnglishLiveTutor.Infrastructure.Ai;
using EnglishLiveTutor.Infrastructure.Lessons;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Domain.Tests;

public class LessonPlayerRegressionTests
{
    [Theory]
    [InlineData(ExerciseType.Dictation, "{}", "{\"referenceTranscript\":\"I work from home.\"}", "I work from home.")]
    [InlineData(ExerciseType.ListeningComprehension, "{\"transcript\":\"Hello, how are you?\"}", "{}", "Hello, how are you?")]
    [InlineData(ExerciseType.Shadowing, "{\"transcript\":\"I start at nine.\"}", "{}", "I start at nine.")]
    [InlineData(ExerciseType.SpellingPractice, "{\"audioText\":\"schedule\"}", "{}", "schedule")]
    public async Task Audio_UsesServerContent_CachesAndDoesNotReturnHiddenText(ExerciseType type, string content, string grading, string expected)
    {
        await using var db = CreateDb();
        var lesson = LessonEntity.Create("Daily routine", "A2", "Remote work");
        var exercise = lesson.AddExercise("one", type, "Practice", "Listen", 5, GradingMode.Deterministic, content, grading);
        var attempt = LessonAttemptEntity.Start(lesson.Id, "learner", 1, 5);
        db.Lessons.Add(lesson); db.LessonAttempts.Add(attempt); await db.SaveChangesAsync();
        var provider = new FakeTts();
        var cache = new FakeAudioRepository();
        var handler = new SynthesizeExerciseAudioCommandHandler(db, new User("learner"), provider, cache, new PronunciationSynthesisLock());
        var result = await handler.HandleAsync(new(attempt.Id, exercise.Id, new(ApiKey: "test-only")));
        Assert.Equal(expected, provider.LastText);
        Assert.NotEmpty(result.AudioBase64);
        string json = JsonSerializer.Serialize(result);
        Assert.DoesNotContain(expected, json);
        Assert.DoesNotContain("SystemPrompt", json);
        // Existing audio can be replayed without sending a provider key.
        await handler.HandleAsync(new(attempt.Id, exercise.Id, new()));
        Assert.Equal(1, provider.Calls);
        Assert.Single(cache.Items);
    }

    [Fact]
    public async Task Audio_DeniesAnotherLearnerBeforeCallingProvider()
    {
        await using var db = CreateDb();
        var lesson = LessonEntity.Create("Daily routine", "A2", "Work");
        var exercise = lesson.AddExercise("one", ExerciseType.Dictation, "Practice", "Listen", 5, GradingMode.Deterministic, "{}", "{\"transcript\":\"Secret\"}");
        var attempt = LessonAttemptEntity.Start(lesson.Id, "owner", 1, 5);
        db.Lessons.Add(lesson); db.LessonAttempts.Add(attempt); await db.SaveChangesAsync();
        var provider = new FakeTts();
        var handler = new SynthesizeExerciseAudioCommandHandler(db, new User("another"), provider, new FakeAudioRepository(), new PronunciationSynthesisLock());
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(new(attempt.Id, exercise.Id, new(ApiKey: "test-only"))));
        Assert.Equal(0, provider.Calls);
    }

    [Fact]
    public void Sanitizer_RemovesNestedAnswers_ButRetainsQuestionAndShadowingReference()
    {
        var sanitizer = new LessonSanitizer();
        var safe = sanitizer.SanitizeContentForLearner(ExerciseType.ReadingComprehension,
            "{\"passage\":\"Read me\",\"subQuestions\":[{\"id\":\"q-1\",\"questionText\":\"Where?\",\"grading\":{\"correctOptionIds\":[\"secret\"]}}]}");
        Assert.Contains("Where?", safe);
        Assert.DoesNotContain("secret", safe);
        Assert.DoesNotContain("grading", safe);
        var shadowing = sanitizer.SanitizeContentForLearner(ExerciseType.Shadowing, "{\"transcript\":\"Repeat this\"}");
        Assert.Contains("referenceText", shadowing);
        Assert.Contains("Repeat this", shadowing);
        var spelling = sanitizer.SanitizeContentForLearner(ExerciseType.SpellingPractice,
            "{\"word\":\"secret\",\"audioText\":\"secret\",\"scrambledLetters\":[\"s\"],\"prompt\":\"A clue\"}");
        Assert.DoesNotContain("secret", spelling);
        Assert.Contains("A clue", spelling);
    }

    [Theory]
    [InlineData(ExerciseType.ReadingComprehension)]
    [InlineData(ExerciseType.ListeningComprehension)]
    public void Comprehension_GradesQuestionIdsAndPartialCredit(ExerciseType type)
    {
        var grader = new DeterministicExerciseGrader(new DictationScorer());
        string content = """
            {"subQuestions":[
              {"id":"q-1","questionKind":"SingleChoice","maxPoints":5,"grading":{"correctOptionIds":["right"]}},
              {"id":"q-2","questionKind":"TrueFalse","maxPoints":5,"grading":{"statementAnswer":false}}
            ]}
            """;
        var result = grader.Grade(type, content, "{}",
            "{\"subQuestionAnswers\":[{\"questionId\":\"q-1\",\"selectedOptionIds\":[\"wrong\"]},{\"questionId\":\"q-2\",\"answer\":false}]}", 10);
        Assert.Equal(50, result.Score100);
        Assert.Equal(5, result.PointsEarned);
    }

    [Fact]
    public void Blanks_UsesCanonicalAnswerMap_AndDoesNotAwardPointsForMissingDefinitions()
    {
        var grader = new DeterministicExerciseGrader(new DictationScorer());
        var result = grader.Grade(ExerciseType.FillInTheBlanks, "{}",
            "{\"blankAnswers\":{\"b1\":{\"acceptedAnswers\":[\"wake up\"]},\"b2\":{\"acceptedAnswers\":[\"get ready\"]}}}",
            "{\"answers\":{\"b1\":\"wake up\",\"b2\":\"wrong\"}}", 10);
        Assert.Equal(50, result.Score100);
        Assert.Equal(5, result.PointsEarned);
        Assert.Throws<InvalidOperationException>(() => grader.Grade(ExerciseType.FillInTheBlanks, "{}", "{}", "{}", 10));
    }

    [Fact]
    public async Task CompositeSubmission_CompletesInsteadOfRemainingPending()
    {
        await using var db = CreateDb();
        var lesson = LessonEntity.Create("Reading", "A2", "Work");
        var exercise = lesson.AddExercise("read", ExerciseType.ReadingComprehension, "Reading", "Choose", 5,
            GradingMode.Composite,
            "{\"subQuestions\":[{\"id\":\"q-1\",\"questionKind\":\"SingleChoice\",\"maxPoints\":5,\"grading\":{\"correctOptionIds\":[\"o-2\"]}}]}");
        var attempt = LessonAttemptEntity.Start(lesson.Id, "learner", 1, 5);
        db.Lessons.Add(lesson); db.LessonAttempts.Add(attempt); await db.SaveChangesAsync();
        var user = new User("learner");
        var resume = new AttemptQueryHandlers(db, user, new LessonSanitizer());
        var handler = new AttemptCommandHandlers(db, user, TimeProvider.System,
            new DeterministicExerciseGrader(new DictationScorer()), new LessonAiGrader(), resume);
        var result = await handler.HandleAsync(new SubmitExerciseAnswerCommand(attempt.Id,
            new SubmitExerciseAnswerRequest(exercise.Id, null, 1,
                JsonNode.Parse("{\"subQuestionAnswers\":[{\"questionId\":\"q-1\",\"selectedOptionIds\":[\"o-2\"]}]}")!)));
        Assert.Equal("Completed", result.Status);
        Assert.Equal(2, result.AttemptRevision);
        Assert.Equal(5, result.PointsEarned);
        Assert.Equal(1, result.AttemptProgress.CompletedExercisesCount);
    }

    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private sealed record User(string? UserId) : ICurrentUser;
    private sealed class FakeTts : ILivePronunciationTtsSynthesizer
    {
        public int Calls;
        public string? LastText;
        public Task<SynthesizePronunciationAudioResponseDto> SynthesizeSpeechAsync(SynthesizePronunciationAudioRequest request, CancellationToken ct = default)
        {
            Calls++; LastText = request.Text;
            return Task.FromResult(new SynthesizePronunciationAudioResponseDto(request.Text!, request.TargetType,
                request.Accent, request.VoiceName, request.SpeechSpeed, request.ModelCode, request.Text!,
                "audio/wav", 24000, 100, Convert.ToBase64String([1, 2, 3, 4]), true));
        }
    }
    private sealed class FakeAudioRepository : IGeneratedPronunciationAudioRepository
    {
        public List<GeneratedPronunciationAudioEntity> Items = [];
        public Task<GeneratedPronunciationAudioEntity?> GetValidAudioAsync(string targetType, Guid targetId, string voiceName,
            decimal speechSpeed, string accent, string modelCode, string textHash, CancellationToken ct = default) =>
            Task.FromResult(Items.LastOrDefault(a => a.TargetType == targetType && a.TargetId == targetId && a.VoiceName == voiceName &&
                a.SpeechSpeed == speechSpeed && a.Accent == accent && a.ModelCode == modelCode && a.TextHash == textHash));
        public Task AddAsync(GeneratedPronunciationAudioEntity entity, CancellationToken ct = default) { Items.Add(entity); return Task.CompletedTask; }
        public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
    }
}
