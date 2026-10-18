using EnglishLiveTutor.Domain.LessonAttempts;
using EnglishLiveTutor.Domain.LessonAttempts.Enums;
using EnglishLiveTutor.Domain.Lessons;
using EnglishLiveTutor.Domain.Lessons.Enums;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class LessonPersistenceTests
{
    private static AppDbContext CreateContext(string dbName)
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var user = new FakeCurrentUser { UserId = "teacher-1" };
        var interceptor = new AuditingSaveChangesInterceptor(clock, user);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(interceptor)
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task LessonEntity_WithExercises_ShouldPersistAndQueryCorrectly()
    {
        string dbName = Guid.NewGuid().ToString("N");
        var lessonId = Guid.NewGuid();

        await using (var db = CreateContext(dbName))
        {
            var lesson = LessonEntity.Create(
                title: "At the Airport",
                cefrLevel: "A2",
                topic: "Travel",
                content: "Airport navigation essentials.",
                estimatedMinutes: 20,
                id: lessonId);

            lesson.AddExercise(
                key: "ex-vocab",
                type: ExerciseType.Flashcards,
                title: "Airport Vocab",
                instructions: "Review flashcards",
                maxPoints: 0,
                gradingMode: GradingMode.SelfAssessment,
                contentJson: "{\"cards\":[{\"id\":\"c1\",\"term\":\"boarding pass\"}]}");

            lesson.AddExercise(
                key: "ex-mcq",
                type: ExerciseType.MultipleChoice,
                title: "Gate Announcement",
                instructions: "Select the gate number",
                maxPoints: 2m,
                gradingMode: GradingMode.Deterministic,
                contentJson: "{\"question\":\"Which gate?\"}",
                gradingDefinitionJson: "{\"correctOptionIds\":[\"opt-a\"]}");

            db.Lessons.Add(lesson);
            await db.SaveChangesAsync();
        }

        await using (var db = CreateContext(dbName))
        {
            var saved = await db.Lessons
                .Include(l => l.Exercises)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            Assert.NotNull(saved);
            Assert.Equal("At the Airport", saved.Title);
            Assert.Equal("A2", saved.CefrLevel);
            Assert.Equal(2, saved.Exercises.Count);

            var ex1 = saved.Exercises.First(e => e.Key == "ex-vocab");
            Assert.Equal(ExerciseType.Flashcards, ex1.Type);
            Assert.Equal(GradingMode.SelfAssessment, ex1.GradingMode);
            Assert.Equal(1, ex1.Order);

            var ex2 = saved.Exercises.First(e => e.Key == "ex-mcq");
            Assert.Equal(ExerciseType.MultipleChoice, ex2.Type);
            Assert.Equal(GradingMode.Deterministic, ex2.GradingMode);
            Assert.Equal(2, ex2.Order);
            Assert.Equal(2m, ex2.MaxPoints);
        }
    }

    [Fact]
    public async Task LessonAttempt_WithAnswers_ShouldPersistAndQueryCorrectly()
    {
        string dbName = Guid.NewGuid().ToString("N");
        var attemptId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var exId = Guid.NewGuid();

        await using (var db = CreateContext(dbName))
        {
            var attempt = LessonAttemptEntity.Start(
                lessonId: lessonId,
                userId: "learner-42",
                pinnedLessonVersion: 1,
                totalMaxPoints: 10m,
                id: attemptId);

            var answer = attempt.SubmitAnswer(
                exerciseId: exId,
                subQuestionId: null,
                expectedRevision: 1,
                gradingMode: GradingMode.Deterministic,
                maxPoints: 5m,
                answerPayloadJson: "{\"selectedOptionIds\":[\"opt-2\"]}");

            attempt.ApplyGradingResult(
                answer.Id,
                SubmissionGradingStatus.Completed,
                score100: 100m,
                pointsEarned: 5m,
                gradingResultJson: "{\"explanation\":\"Correct!\"}",
                DateTimeOffset.UtcNow);

            db.LessonAttempts.Add(attempt);
            await db.SaveChangesAsync();
        }

        await using (var db = CreateContext(dbName))
        {
            var saved = await db.LessonAttempts
                .Include(a => a.Answers)
                .FirstOrDefaultAsync(a => a.Id == attemptId);

            Assert.NotNull(saved);
            Assert.Equal("learner-42", saved.UserId);
            Assert.Equal(2, saved.AttemptRevision);
            Assert.Equal(5m, saved.CurrentSubtotalPoints);
            Assert.Single(saved.Answers);

            var ans = saved.Answers.First();
            Assert.Equal(exId, ans.ExerciseId);
            Assert.Equal(SubmissionGradingStatus.Completed, ans.Status);
            Assert.Equal(100m, ans.Score100);
            Assert.Equal(5m, ans.PointsEarned);
        }
    }

    [Fact]
    public async Task LessonRubricSeeder_ShouldSeedAll10RubricsIdempotentlyWithDescriptors()
    {
        string dbName = Guid.NewGuid().ToString("N");

        await using (var db = CreateContext(dbName))
        {
            await LessonRubricSeeder.SeedAsync(db);
        }

        await using (var db = CreateContext(dbName))
        {
            var rubrics = await db.ScoringRubrics
                .Include(r => r.Criteria)
                .Where(r => r.AssessmentScope == "LessonExercise")
                .ToListAsync();

            Assert.Equal(10, rubrics.Count);

            // Verify Short Answer template
            var shortAnswer = rubrics.FirstOrDefault(r => r.Code == "LESSON_SHORT_ANSWER");
            Assert.NotNull(shortAnswer);
            Assert.Equal("ShortAnswer", shortAnswer.SubQuestionKind);
            Assert.Contains("ReadingComprehension", shortAnswer.ApplicableExerciseTypes ?? "");
            Assert.Contains("ListeningComprehension", shortAnswer.ApplicableExerciseTypes ?? "");
            Assert.Equal(3, shortAnswer.Criteria.Count);

            // Verify descriptors level 0-4 are populated on every criterion
            foreach (var r in rubrics)
            {
                Assert.True(r.Criteria.Count > 0);
                foreach (var c in r.Criteria)
                {
                    Assert.False(string.IsNullOrWhiteSpace(c.DescriptorLevel0), $"{r.Code} - {c.Code} missing level0");
                    Assert.False(string.IsNullOrWhiteSpace(c.DescriptorLevel1), $"{r.Code} - {c.Code} missing level1");
                    Assert.False(string.IsNullOrWhiteSpace(c.DescriptorLevel2), $"{r.Code} - {c.Code} missing level2");
                    Assert.False(string.IsNullOrWhiteSpace(c.DescriptorLevel3), $"{r.Code} - {c.Code} missing level3");
                    Assert.False(string.IsNullOrWhiteSpace(c.DescriptorLevel4), $"{r.Code} - {c.Code} missing level4");
                }
            }

            // Verify idempotency: running again should not duplicate rubrics
            await LessonRubricSeeder.SeedAsync(db);

            var countAfterSecondSeed = await db.ScoringRubrics
                .CountAsync(r => r.AssessmentScope == "LessonExercise");
            Assert.Equal(10, countAfterSecondSeed);
        }
    }
}
