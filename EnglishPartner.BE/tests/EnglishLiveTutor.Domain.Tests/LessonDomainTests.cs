using EnglishLiveTutor.Domain.LessonAttempts;
using EnglishLiveTutor.Domain.LessonAttempts.Enums;
using EnglishLiveTutor.Domain.Lessons;
using EnglishLiveTutor.Domain.Lessons.Enums;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class LessonDomainTests
{
    [Fact]
    public void ExerciseType_ShouldContainAll16TypesWithExactOrdinalValues()
    {
        Assert.Equal(1, (int)ExerciseType.Flashcards);
        Assert.Equal(2, (int)ExerciseType.SpeakingPractice);
        Assert.Equal(3, (int)ExerciseType.MultipleChoice);
        Assert.Equal(4, (int)ExerciseType.FillInTheBlanks);
        Assert.Equal(5, (int)ExerciseType.Matching);
        Assert.Equal(6, (int)ExerciseType.SpellingPractice);
        Assert.Equal(7, (int)ExerciseType.SentenceUnscramble);
        Assert.Equal(8, (int)ExerciseType.ErrorCorrection);
        Assert.Equal(9, (int)ExerciseType.Translation);
        Assert.Equal(10, (int)ExerciseType.TrueFalse);
        Assert.Equal(11, (int)ExerciseType.ReadingComprehension);
        Assert.Equal(12, (int)ExerciseType.ListeningComprehension);
        Assert.Equal(13, (int)ExerciseType.Dictation);
        Assert.Equal(14, (int)ExerciseType.Shadowing);
        Assert.Equal(15, (int)ExerciseType.ConversationRolePlay);
        Assert.Equal(16, (int)ExerciseType.WritingPractice);
        Assert.Equal(16, Enum.GetValues<ExerciseType>().Length);
    }

    [Fact]
    public void LessonCreate_ShouldInitializeWithDraftStatusAndVersionOne()
    {
        var lesson = LessonEntity.Create(
            title: "Restaurant English",
            cefrLevel: "B1",
            topic: "Dining Out",
            content: "Learn how to order food politely.",
            estimatedMinutes: 25);

        Assert.Equal("Restaurant English", lesson.Title);
        Assert.Equal("B1", lesson.CefrLevel);
        Assert.Equal("Dining Out", lesson.Topic);
        Assert.Equal(LessonStatus.Draft, lesson.Status);
        Assert.Equal(1, lesson.Version);
        Assert.Null(lesson.ParentLessonId);
        Assert.Empty(lesson.Exercises);
    }

    [Fact]
    public void LessonCreate_WithInvalidCefr_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            LessonEntity.Create("Title", "Z9", "Topic"));
    }

    [Fact]
    public void AddExercise_ShouldAddAndNormalizeOrder()
    {
        var lesson = LessonEntity.Create("Lesson 1", "A2", "Travel");

        var ex1 = lesson.AddExercise(
            key: "ex-1",
            type: ExerciseType.Flashcards,
            title: "Vocab",
            instructions: "Review words",
            maxPoints: 0,
            gradingMode: GradingMode.SelfAssessment,
            contentJson: "{\"cards\":[]}");

        var ex2 = lesson.AddExercise(
            key: "ex-2",
            type: ExerciseType.MultipleChoice,
            title: "Quiz",
            instructions: "Choose best answer",
            maxPoints: 2,
            gradingMode: GradingMode.Deterministic,
            contentJson: "{\"question\":\"...\"}",
            gradingDefinitionJson: "{\"correctOptionIds\":[\"opt-1\"]}");

        Assert.Equal(2, lesson.Exercises.Count);
        Assert.Equal(1, ex1.Order);
        Assert.Equal(2, ex2.Order);
    }

    [Fact]
    public void AddExercise_WithDuplicateKey_ShouldThrowInvalidOperationException()
    {
        var lesson = LessonEntity.Create("Lesson 1", "A2", "Travel");
        lesson.AddExercise("ex-dup", ExerciseType.Flashcards, "V1", "Inst", 0, GradingMode.SelfAssessment, "{}");

        Assert.Throws<InvalidOperationException>(() =>
            lesson.AddExercise("ex-dup", ExerciseType.MultipleChoice, "V2", "Inst", 2, GradingMode.Deterministic, "{}"));
    }

    [Fact]
    public void ReorderExercises_ShouldUpdateOrdersSequentially()
    {
        var lesson = LessonEntity.Create("Lesson 1", "B1", "Work");
        var ex1 = lesson.AddExercise("ex-1", ExerciseType.Flashcards, "F", "I", 0, GradingMode.SelfAssessment, "{}");
        var ex2 = lesson.AddExercise("ex-2", ExerciseType.Translation, "T", "I", 5, GradingMode.AiRubric, "{}");

        Assert.Equal(1, ex1.Order);
        Assert.Equal(2, ex2.Order);

        lesson.ReorderExercises(new[] { ex2.Id, ex1.Id });

        Assert.Equal(1, ex2.Order);
        Assert.Equal(2, ex1.Order);
    }

    [Fact]
    public void RemoveExercise_ShouldRemoveAndRenormalizeRemainingOrders()
    {
        var lesson = LessonEntity.Create("Lesson 1", "B1", "Work");
        var ex1 = lesson.AddExercise("ex-1", ExerciseType.Flashcards, "F", "I", 0, GradingMode.SelfAssessment, "{}");
        var ex2 = lesson.AddExercise("ex-2", ExerciseType.MultipleChoice, "M", "I", 2, GradingMode.Deterministic, "{}");
        var ex3 = lesson.AddExercise("ex-3", ExerciseType.Dictation, "D", "I", 5, GradingMode.Deterministic, "{}");

        lesson.RemoveExercise(ex2.Id);

        Assert.Equal(2, lesson.Exercises.Count);
        Assert.Equal(1, ex1.Order);
        Assert.Equal(2, ex3.Order);
    }

    [Fact]
    public void Publish_WithoutExercises_ShouldThrowInvalidOperationException()
    {
        var lesson = LessonEntity.Create("Empty Lesson", "B1", "General");

        Assert.Throws<InvalidOperationException>(() =>
            lesson.Publish(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Publish_WithValidExercises_ShouldSetStatusPublished()
    {
        var lesson = LessonEntity.Create("Ready Lesson", "B1", "General");
        lesson.AddExercise("ex-1", ExerciseType.MultipleChoice, "M", "I", 2, GradingMode.Deterministic, "{}");

        var publishTime = DateTimeOffset.UtcNow;
        lesson.Publish(publishTime);

        Assert.Equal(LessonStatus.Published, lesson.Status);
        Assert.Equal(publishTime, lesson.PublishedAtUtc);
    }

    [Fact]
    public void Publish_Twice_ShouldThrowInvalidOperationException()
    {
        var lesson = LessonEntity.Create("Ready Lesson", "B1", "General");
        lesson.AddExercise("ex-1", ExerciseType.MultipleChoice, "M", "I", 2, GradingMode.Deterministic, "{}");
        lesson.Publish(DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            lesson.Publish(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void MutationOnPublishedLesson_ShouldThrowInvalidOperationException()
    {
        var lesson = LessonEntity.Create("Published Lesson", "B1", "General");
        lesson.AddExercise("ex-1", ExerciseType.MultipleChoice, "M", "I", 2, GradingMode.Deterministic, "{}");
        lesson.Publish(DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            lesson.AddExercise("ex-2", ExerciseType.Flashcards, "F", "I", 0, GradingMode.SelfAssessment, "{}"));

        Assert.Throws<InvalidOperationException>(() =>
            lesson.UpdateDraft("New Title", "B1", "General", null, 30, null, null, null));
    }

    [Fact]
    public void CreateDraftRevision_ShouldCreateNewDraftWithIncrementedVersion()
    {
        var lesson = LessonEntity.Create("Published Lesson", "B1", "General");
        lesson.AddExercise("ex-1", ExerciseType.MultipleChoice, "M", "I", 2, GradingMode.Deterministic, "{}");
        lesson.Publish(DateTimeOffset.UtcNow);

        var revisionId = Guid.NewGuid();
        var revision = lesson.CreateDraftRevision(revisionId, DateTimeOffset.UtcNow);

        Assert.Equal(revisionId, revision.Id);
        Assert.Equal(lesson.Id, revision.ParentLessonId);
        Assert.Equal(LessonStatus.Draft, revision.Status);
        Assert.Equal(2, revision.Version);
        Assert.Single(revision.Exercises);
        Assert.NotEqual(lesson.Exercises.First().Id, revision.Exercises.First().Id);
        Assert.Equal("ex-1", revision.Exercises.First().Key);
    }

    [Fact]
    public void LessonAttempt_Start_ShouldInitializeWithRevisionOneAndInProgress()
    {
        var lessonId = Guid.NewGuid();
        var attempt = LessonAttemptEntity.Start(lessonId, "user-123", pinnedLessonVersion: 1, totalMaxPoints: 20m);

        Assert.Equal(lessonId, attempt.LessonId);
        Assert.Equal("user-123", attempt.UserId);
        Assert.Equal(1, attempt.PinnedLessonVersion);
        Assert.Equal(1, attempt.AttemptRevision);
        Assert.Equal(LessonAttemptStatus.InProgress, attempt.Status);
        Assert.Equal(0m, attempt.CurrentSubtotalPoints);
        Assert.Equal(20m, attempt.TotalMaxPoints);
        Assert.Null(attempt.FinalScore100);
    }

    [Fact]
    public void SubmitAnswer_WithCorrectRevision_ShouldIncrementRevisionAndStoreAnswer()
    {
        var attempt = LessonAttemptEntity.Start(Guid.NewGuid(), "user-1", 1, 10m);
        var exerciseId = Guid.NewGuid();

        var answer = attempt.SubmitAnswer(
            exerciseId: exerciseId,
            subQuestionId: null,
            expectedRevision: 1,
            gradingMode: GradingMode.Deterministic,
            maxPoints: 5m,
            answerPayloadJson: "{\"selectedOptionIds\":[\"opt-b\"]}");

        Assert.NotNull(answer);
        Assert.Equal(2, attempt.AttemptRevision);
        Assert.Single(attempt.Answers);
        Assert.Equal(SubmissionGradingStatus.Pending, answer.Status);
    }

    [Fact]
    public void SubmitAnswer_WithMismatchedRevision_ShouldThrowInvalidOperationException()
    {
        var attempt = LessonAttemptEntity.Start(Guid.NewGuid(), "user-1", 1, 10m);
        var exerciseId = Guid.NewGuid();

        Assert.Throws<InvalidOperationException>(() =>
            attempt.SubmitAnswer(
                exerciseId: exerciseId,
                subQuestionId: null,
                expectedRevision: 999,
                gradingMode: GradingMode.Deterministic,
                maxPoints: 5m,
                answerPayloadJson: "{}"));
    }

    [Fact]
    public void ApplyGradingResult_ShouldRecalculateSubtotalPoints()
    {
        var attempt = LessonAttemptEntity.Start(Guid.NewGuid(), "user-1", 1, 10m);
        var ex1 = Guid.NewGuid();
        var ex2 = Guid.NewGuid();

        var ans1 = attempt.SubmitAnswer(ex1, null, 1, GradingMode.Deterministic, 5m, "{}");
        var ans2 = attempt.SubmitAnswer(ex2, null, 2, GradingMode.AiRubric, 5m, "{}");

        Assert.Equal(3, attempt.AttemptRevision);

        // Grade ans1: 100% -> 5 points
        attempt.ApplyGradingResult(ans1.Id, SubmissionGradingStatus.Completed, 100m, 5m, "{}", DateTimeOffset.UtcNow);
        Assert.Equal(5m, attempt.CurrentSubtotalPoints);

        // Grade ans2: 96.25% -> 4.81 points
        attempt.ApplyGradingResult(ans2.Id, SubmissionGradingStatus.Completed, 96.25m, 4.81m, "{}", DateTimeOffset.UtcNow);
        Assert.Equal(9.81m, attempt.CurrentSubtotalPoints);
    }

    [Fact]
    public void RetryGrading_OnFailedAnswer_ShouldResetToPendingAndIncrementRevision()
    {
        var attempt = LessonAttemptEntity.Start(Guid.NewGuid(), "user-1", 1, 10m);
        var ex1 = Guid.NewGuid();
        var ans1 = attempt.SubmitAnswer(ex1, null, 1, GradingMode.AiRubric, 5m, "{}");

        attempt.ApplyGradingResult(ans1.Id, SubmissionGradingStatus.Failed, null, null, "{\"error\":\"AI timeout\"}", DateTimeOffset.UtcNow);

        Assert.Equal(2, attempt.AttemptRevision);
        Assert.Equal(SubmissionGradingStatus.Failed, ans1.Status);

        attempt.RetryGrading(ans1.Id, expectedRevision: 2);

        Assert.Equal(3, attempt.AttemptRevision);
        Assert.Equal(SubmissionGradingStatus.Pending, ans1.Status);
    }

    [Fact]
    public void RetryGrading_OnCompletedAnswer_ShouldThrowInvalidOperationException()
    {
        var attempt = LessonAttemptEntity.Start(Guid.NewGuid(), "user-1", 1, 10m);
        var ex1 = Guid.NewGuid();
        var ans1 = attempt.SubmitAnswer(ex1, null, 1, GradingMode.Deterministic, 5m, "{}");
        attempt.ApplyGradingResult(ans1.Id, SubmissionGradingStatus.Completed, 100m, 5m, "{}", DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            attempt.RetryGrading(ans1.Id, expectedRevision: 2));
    }

    [Fact]
    public void CompleteAttempt_ShouldCalculateFinalScore100WithRounding()
    {
        var attempt = LessonAttemptEntity.Start(Guid.NewGuid(), "user-1", 1, 20m);
        var ex1 = Guid.NewGuid();
        var ex2 = Guid.NewGuid();

        var ans1 = attempt.SubmitAnswer(ex1, null, 1, GradingMode.Deterministic, 10m, "{}");
        var ans2 = attempt.SubmitAnswer(ex2, null, 2, GradingMode.AiRubric, 10m, "{}");

        attempt.ApplyGradingResult(ans1.Id, SubmissionGradingStatus.Completed, 100m, 10m, "{}", DateTimeOffset.UtcNow);
        attempt.ApplyGradingResult(ans2.Id, SubmissionGradingStatus.Completed, 96.25m, 9.63m, "{}", DateTimeOffset.UtcNow);

        attempt.Complete(DateTimeOffset.UtcNow);

        Assert.Equal(LessonAttemptStatus.Completed, attempt.Status);
        // Subtotal = 19.63 out of 20.00 -> 19.63 / 20 * 100 = 98.15%
        Assert.Equal(19.63m, attempt.CurrentSubtotalPoints);
        Assert.Equal(98.15m, attempt.FinalScore100);
    }
}
