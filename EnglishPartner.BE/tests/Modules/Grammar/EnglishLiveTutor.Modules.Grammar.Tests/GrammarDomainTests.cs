using EnglishLiveTutor.Domain.Grammar;
using EnglishLiveTutor.Domain.Grammar.Enums;
using EnglishLiveTutor.Domain.Common.Exercises;

namespace EnglishLiveTutor.Domain.Tests;

public class GrammarDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 10, 0, 0, TimeSpan.Zero);
    private const string ValidTheory = "{\"objectives\":[\"Test\"],\"sections\":[{\"heading\":\"H\",\"explanationVi\":\"E\"}],\"commonMistakes\":[{\"wrong\":\"W\",\"right\":\"R\",\"explanationVi\":\"E\"}],\"summary\":[\"S\"]}";

    [Theory]
    [InlineData("A1-1", "A1", 1)] // Not 2 digits
    [InlineData("a1-01", "A1", 1)] // Lowercase
    [InlineData("D1-01", "D1", 1)] // Invalid level prefix
    [InlineData("A1-02", "A2", 2)] // Level mismatch
    [InlineData("A1-02", "A1", 3)] // Order mismatch
    public void GrammarStep_InvalidCodeLevelOrder_ThrowsArgumentException(string code, string level, int order)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            GrammarStepEntity.Create(code, level, order, "Từ loại", "Parts of Speech", "Tóm tắt", ValidTheory));
    }

    [Fact]
    public void GrammarStep_LevelHelpers_NavigateCorrectly()
    {
        Assert.Null(GrammarStepEntity.PreviousLevel("A1"));
        Assert.Equal("A1", GrammarStepEntity.PreviousLevel("A2"));
        Assert.Equal("C1", GrammarStepEntity.PreviousLevel("C2"));

        Assert.Equal("A2", GrammarStepEntity.NextLevel("A1"));
        Assert.Equal("C2", GrammarStepEntity.NextLevel("C1"));
        Assert.Null(GrammarStepEntity.NextLevel("C2"));
    }

    [Fact]
    public void GrammarStep_ApplyPackTheory_DoesNotOverwriteWhenEditedByAdminOrLowerVersion()
    {
        var step = GrammarStepEntity.Create("A1-01", "A1", 1, "Bài 1", "Lesson 1", "Tóm tắt", "{\"ver\":1}", theoryVersion: 1);

        // Lower or equal version is rejected
        bool appliedLower = step.ApplyPackTheory("{\"ver\":1}", 1);
        Assert.False(appliedLower);
        Assert.Equal("{\"ver\":1}", step.TheoryJson);

        // Higher version applied
        bool appliedHigher = step.ApplyPackTheory("{\"ver\":2}", 2);
        Assert.True(appliedHigher);
        Assert.Equal("{\"ver\":2}", step.TheoryJson);
        Assert.Equal(2, step.TheoryVersion);

        // Admin edits
        step.EditTheoryByAdmin("{\"ver\":3-admin}");
        Assert.True(step.TheoryEditedByAdmin);
        Assert.Equal(3, step.TheoryVersion);
        Assert.Equal("{\"ver\":3-admin}", step.TheoryJson);

        // Pack with even higher version cannot overwrite admin edits
        bool appliedAfterAdmin = step.ApplyPackTheory("{\"ver\":4}", 4);
        Assert.False(appliedAfterAdmin);
        Assert.Equal("{\"ver\":3-admin}", step.TheoryJson);
        Assert.Equal(3, step.TheoryVersion);
    }

    [Fact]
    public void GrammarExercise_ModerationLifecycle_TransitionsCorrectly()
    {
        var stepId = Guid.NewGuid();
        string hash = new('a', 64);

        // 1. User owned Public exercise starts as Pending
        var pubEx = GrammarExerciseEntity.CreateUserOwned(
            stepId, "user-1", GrammarExerciseSource.UserAi, GrammarExerciseVisibility.Public,
            ExerciseType.MultipleChoice, 1, "Title", "Instr", "{}", null, 10m, GradingMode.Deterministic, hash, Now);

        Assert.Equal(GrammarModerationStatus.Pending, pubEx.Moderation);
        Assert.Equal("user-1", pubEx.OwnerUserId);
        Assert.Equal("user-1", pubEx.AuthorUserId);
        Assert.False(pubEx.IsCore);

        // 2. User owned Private exercise starts as None
        var privEx = GrammarExerciseEntity.CreateUserOwned(
            stepId, "user-1", GrammarExerciseSource.UserAi, GrammarExerciseVisibility.Private,
            ExerciseType.MultipleChoice, 1, "Title", "Instr", "{}", null, 10m, GradingMode.Deterministic, hash, Now);

        Assert.Equal(GrammarModerationStatus.None, privEx.Moderation);

        // 3. Changing Private to Public becomes Pending
        privEx.ChangeVisibility(GrammarExerciseVisibility.Public, Now);
        Assert.Equal(GrammarModerationStatus.Pending, privEx.Moderation);

        // 4. Approve Pending status
        pubEx.Approve("admin-1", Now);
        Assert.Equal(GrammarModerationStatus.Approved, pubEx.Moderation);
        Assert.Equal("admin-1", pubEx.ReviewedBy);

        // Already approved cannot be rejected or approved again
        Assert.Throws<InvalidOperationException>(() => pubEx.Reject("admin-2", "Reason", Now));
        Assert.Throws<InvalidOperationException>(() => pubEx.Approve("admin-2", Now));

        // 5. Editing approved public exercise returns to Pending
        pubEx.EditContent(ExerciseType.MultipleChoice, 1, "New Title", "Instr", "{}", null, 10m, GradingMode.Deterministic, hash, Now);
        Assert.Equal(GrammarModerationStatus.Pending, pubEx.Moderation);

        // 6. Reject Pending exercise
        Assert.Throws<ArgumentException>(() => pubEx.Reject("admin-1", "", Now));
        pubEx.Reject("admin-1", "Needs better options", Now);
        Assert.Equal(GrammarModerationStatus.Rejected, pubEx.Moderation);
        Assert.Equal("Needs better options", pubEx.RejectReason);

        // 7. Make Private clears reject reason
        pubEx.ChangeVisibility(GrammarExerciseVisibility.Private, Now);
        Assert.Equal(GrammarModerationStatus.None, pubEx.Moderation);
        Assert.Null(pubEx.RejectReason);
    }

    [Fact]
    public void GrammarExercise_VisibilityHelper_EnforcesAccessRules()
    {
        var stepId = Guid.NewGuid();
        string hash = new('b', 64);

        var official = GrammarExerciseEntity.CreateOfficial(
            stepId, "A1-01-E01", ExerciseType.MultipleChoice, 1, "T", "I", "{}", null, 10m, GradingMode.Deterministic, true, hash, Now);

        var userPubPending = GrammarExerciseEntity.CreateUserOwned(
            stepId, "user-a", GrammarExerciseSource.UserAi, GrammarExerciseVisibility.Public,
            ExerciseType.MultipleChoice, 1, "T", "I", "{}", null, 10m, GradingMode.Deterministic, hash, Now);

        var userPubApproved = GrammarExerciseEntity.CreateUserOwned(
            stepId, "user-a", GrammarExerciseSource.UserAi, GrammarExerciseVisibility.Public,
            ExerciseType.MultipleChoice, 1, "T", "I", "{}", null, 10m, GradingMode.Deterministic, hash, Now);
        userPubApproved.Approve("admin-1", Now);

        var userPrivate = GrammarExerciseEntity.CreateUserOwned(
            stepId, "user-a", GrammarExerciseSource.UserAi, GrammarExerciseVisibility.Private,
            ExerciseType.MultipleChoice, 1, "T", "I", "{}", null, 10m, GradingMode.Deterministic, hash, Now);

        // Official is visible to anyone
        Assert.True(official.IsVisibleTo(null));
        Assert.True(official.IsVisibleTo("user-b"));

        // Approved is visible to anyone
        Assert.True(userPubApproved.IsVisibleTo(null));
        Assert.True(userPubApproved.IsVisibleTo("user-b"));

        // Pending and Private are visible to owner ONLY
        Assert.True(userPubPending.IsVisibleTo("user-a"));
        Assert.False(userPubPending.IsVisibleTo("user-b"));
        Assert.False(userPubPending.IsVisibleTo(null));

        Assert.True(userPrivate.IsVisibleTo("user-a"));
        Assert.False(userPrivate.IsVisibleTo("user-b"));
        Assert.False(userPrivate.IsVisibleTo(null));
    }

    [Fact]
    public void GrammarExercise_PromoteToOfficial_PreservesAuthorUserId()
    {
        var stepId = Guid.NewGuid();
        string hash = new('c', 64);

        var ex = GrammarExerciseEntity.CreateUserOwned(
            stepId, "user-author", GrammarExerciseSource.UserAi, GrammarExerciseVisibility.Public,
            ExerciseType.MultipleChoice, 1, "T", "I", "{}", null, 10m, GradingMode.Deterministic, hash, Now);

        // Cannot promote unless approved
        Assert.Throws<InvalidOperationException>(() => ex.PromoteToOfficial("A1-01-E99", true, Now));

        ex.Approve("admin-1", Now);
        ex.PromoteToOfficial("A1-01-E99", isCore: true, Now);

        Assert.Equal(GrammarExerciseSource.Official, ex.Source);
        Assert.Equal("A1-01-E99", ex.Key);
        Assert.Null(ex.OwnerUserId);
        Assert.Equal("user-author", ex.AuthorUserId);
        Assert.True(ex.IsCore);
        Assert.Equal(GrammarExerciseVisibility.Public, ex.Visibility);
        Assert.Equal(GrammarModerationStatus.None, ex.Moderation);
    }

    [Fact]
    public void UserGrammarStepProgress_PassedNeverRevertsToUnlocked()
    {
        var stepId = Guid.NewGuid();
        var progress = UserGrammarStepProgressEntity.Create("user-1", stepId, Now);

        Assert.Equal(GrammarStepProgressStatus.Unlocked, progress.Status);
        Assert.Null(progress.PassedBy);

        // Score below pass threshold (e.g. 70)
        progress.RecordScore(65m, 70m, Now);
        Assert.Equal(GrammarStepProgressStatus.Unlocked, progress.Status);
        Assert.Equal(65m, progress.BestScore100);

        // Score at or above threshold
        progress.RecordScore(85m, 70m, Now);
        Assert.Equal(GrammarStepProgressStatus.Passed, progress.Status);
        Assert.Equal(GrammarStepPassedBy.Steps, progress.PassedBy);
        Assert.Equal(85m, progress.BestScore100);
        Assert.NotNull(progress.PassedAtUtc);

        // Lower score does not revert status or reduce best score
        progress.RecordScore(50m, 70m, Now.AddHours(1));
        Assert.Equal(GrammarStepProgressStatus.Passed, progress.Status);
        Assert.Equal(85m, progress.BestScore100);

        // Mark passed by level test keeps Passed status and preserves original PassedBy and PassedAtUtc
        var originalPassedAt = progress.PassedAtUtc;
        progress.MarkPassedByLevelTest(Now.AddHours(2));
        Assert.Equal(GrammarStepProgressStatus.Passed, progress.Status);
        Assert.Equal(GrammarStepPassedBy.Steps, progress.PassedBy);
        Assert.Equal(originalPassedAt, progress.PassedAtUtc);

        // When not yet passed, MarkPassedByLevelTest sets Passed and LevelTest
        var freshProgress = UserGrammarStepProgressEntity.Create("user-2", stepId, Now);
        freshProgress.MarkPassedByLevelTest(Now.AddHours(1));
        Assert.Equal(GrammarStepProgressStatus.Passed, freshProgress.Status);
        Assert.Equal(GrammarStepPassedBy.LevelTest, freshProgress.PassedBy);
        Assert.Equal(Now.AddHours(1), freshProgress.PassedAtUtc);
    }

    [Fact]
    public void GrammarAttempt_EnforcesSubmissionRules_AndCompleteOnce()
    {
        var e1 = Guid.NewGuid();
        var e2 = Guid.NewGuid();
        var outsideEx = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        string hash = new('d', 64);

        var attempt = GrammarAttemptEntity.Start(
            "user-1", GrammarAttemptKind.StepCore, stepId, "A1", [e1, e2], Now);

        // Rejects submission of exercise not in attempt
        Assert.Throws<InvalidOperationException>(() =>
            attempt.SubmitAnswer(outsideEx, "{}", "Completed", 100m, 10m, 10m, null, hash, Now));

        // Submits answer for e1
        var ans1 = attempt.SubmitAnswer(e1, "{\"ans\":1}", "Completed", 80m, 8m, 10m, "Good", hash, Now);
        Assert.Single(attempt.Answers);
        Assert.Equal("{\"ans\":1}", ans1.AnswerPayloadJson);
        Assert.Equal(80m, ans1.Score100);

        // In StepCore, resubmitting e1 throws grammar_answer_locked
        var lockEx = Assert.Throws<InvalidOperationException>(() =>
            attempt.SubmitAnswer(e1, "{\"ans\":2}", "Completed", 100m, 10m, 10m, "Perfect", hash, Now.AddMinutes(5)));
        Assert.Equal("grammar_answer_locked", lockEx.Message);

        // In Practice, resubmitting e1 replaces answer
        var practiceAttempt = GrammarAttemptEntity.Start("user-1", GrammarAttemptKind.Practice, stepId, "A1", [e1], Now);
        practiceAttempt.SubmitAnswer(e1, "{\"ans\":1}", "Completed", 80m, 8m, 10m, "Good", hash, Now);
        var ans1v2 = practiceAttempt.SubmitAnswer(e1, "{\"ans\":2}", "Completed", 100m, 10m, 10m, "Perfect", hash, Now.AddMinutes(5));
        Assert.Single(practiceAttempt.Answers);
        Assert.Equal("{\"ans\":2}", ans1v2.AnswerPayloadJson);
        Assert.Equal(100m, ans1v2.Score100);

        // Completing attempt
        attempt.Complete(90m, Now.AddMinutes(10));
        Assert.Equal(90m, attempt.Score100);
        Assert.NotNull(attempt.CompletedAtUtc);

        // Submitting after completion fails
        Assert.Throws<InvalidOperationException>(() =>
            attempt.SubmitAnswer(e2, "{}", "Completed", 100m, 10m, 10m, null, hash, Now.AddMinutes(11)));

        // Completing again fails
        Assert.Throws<InvalidOperationException>(() =>
            attempt.Complete(95m, Now.AddMinutes(12)));
    }

    [Fact]
    public void GrammarExercise_ChangeVisibility_SameVisibility_DoesNotTouchModeration()
    {
        var stepId = Guid.NewGuid();
        string hash = new('e', 64);

        var pubEx = GrammarExerciseEntity.CreateUserOwned(
            stepId, "user-1", GrammarExerciseSource.UserAi, GrammarExerciseVisibility.Public,
            ExerciseType.MultipleChoice, 1, "Title", "Instr", "{}", null, 10m, GradingMode.Deterministic, hash, Now);

        pubEx.Approve("admin-1", Now);
        Assert.Equal(GrammarModerationStatus.Approved, pubEx.Moderation);
        Assert.Equal("admin-1", pubEx.ReviewedBy);

        // Calling ChangeVisibility with the same Public visibility must NOT touch moderation or reset to Pending
        pubEx.ChangeVisibility(GrammarExerciseVisibility.Public, Now.AddHours(1));
        Assert.Equal(GrammarModerationStatus.Approved, pubEx.Moderation);
        Assert.Equal("admin-1", pubEx.ReviewedBy);
    }

    [Fact]
    public void GrammarExercise_KeyAndContentHashValidation()
    {
        var stepId = Guid.NewGuid();
        string validHash = new('f', 64);
        string longKey = new('k', 65);

        // Key > 64 in CreateOfficial
        Assert.Throws<ArgumentException>(() =>
            GrammarExerciseEntity.CreateOfficial(stepId, longKey, ExerciseType.MultipleChoice, 1, "T", "I", "{}", null, 10m, GradingMode.Deterministic, true, validHash, Now));

        // Key > 64 in PromoteToOfficial
        var ex = GrammarExerciseEntity.CreateUserOwned(
            stepId, "user-1", GrammarExerciseSource.UserAi, GrammarExerciseVisibility.Public,
            ExerciseType.MultipleChoice, 1, "T", "I", "{}", null, 10m, GradingMode.Deterministic, validHash, Now);
        ex.Approve("admin-1", Now);
        Assert.Throws<ArgumentException>(() => ex.PromoteToOfficial(longKey, true, Now));

        // ContentHash: length != 64
        Assert.Throws<ArgumentException>(() =>
            GrammarExerciseEntity.CreateOfficial(stepId, "K1", ExerciseType.MultipleChoice, 1, "T", "I", "{}", null, 10m, GradingMode.Deterministic, true, "short", Now));

        // ContentHash: uppercase hex rejected
        string upperHash = "A" + new string('0', 63);
        Assert.Throws<ArgumentException>(() =>
            GrammarExerciseEntity.CreateOfficial(stepId, "K1", ExerciseType.MultipleChoice, 1, "T", "I", "{}", null, 10m, GradingMode.Deterministic, true, upperHash, Now));

        // ContentHash: non-hex rejected
        string nonHexHash = "g" + new string('0', 63);
        Assert.Throws<ArgumentException>(() =>
            GrammarExerciseEntity.CreateOfficial(stepId, "K1", ExerciseType.MultipleChoice, 1, "T", "I", "{}", null, 10m, GradingMode.Deterministic, true, nonHexHash, Now));
    }

    [Fact]
    public void GrammarExercise_FocusProperty_TrimmedAndBounded()
    {
        var stepId = Guid.NewGuid();
        string hash = new('1', 64);

        // Empty focus becomes null
        var ex = GrammarExerciseEntity.CreateOfficial(
            stepId, "KEY1", ExerciseType.MultipleChoice, 1, "Title", "Instr", "{}", null, 10m, GradingMode.Deterministic, true, hash, Now, focus: "   ");
        Assert.Null(ex.Focus);

        // Valid focus trimmed
        var ex2 = GrammarExerciseEntity.CreateOfficial(
            stepId, "KEY2", ExerciseType.MultipleChoice, 1, "Title", "Instr", "{}", null, 10m, GradingMode.Deterministic, true, hash, Now, focus: "  Present Simple  ");
        Assert.Equal("Present Simple", ex2.Focus);

        // Focus > 200 throws
        string longFocus = new('x', 201);
        Assert.Throws<ArgumentException>(() =>
            GrammarExerciseEntity.CreateOfficial(stepId, "KEY3", ExerciseType.MultipleChoice, 1, "Title", "Instr", "{}", null, 10m, GradingMode.Deterministic, true, hash, Now, focus: longFocus));

        // EditContent updates focus
        ex2.EditContent(ExerciseType.MultipleChoice, 1, "Title", "Instr", "{}", null, 10m, GradingMode.Deterministic, hash, Now, focus: "Past Simple");
        Assert.Equal("Past Simple", ex2.Focus);
    }

    [Fact]
    public void GrammarAttempt_ValidationRules()
    {
        var stepId = Guid.NewGuid();
        var e1 = Guid.NewGuid();
        string hash = new('2', 64);

        // Invalid CEFR level
        Assert.Throws<ArgumentException>(() =>
            GrammarAttemptEntity.Start("user-1", GrammarAttemptKind.StepCore, stepId, "X1", [e1], Now));

        // Guid.Empty in exerciseIds
        Assert.Throws<ArgumentException>(() =>
            GrammarAttemptEntity.Start("user-1", GrammarAttemptKind.StepCore, stepId, "A1", [Guid.Empty], Now));

        // Duplicate exerciseIds
        Assert.Throws<ArgumentException>(() =>
            GrammarAttemptEntity.Start("user-1", GrammarAttemptKind.StepCore, stepId, "A1", [e1, e1], Now));

        // SubmitAnswer with invalid status
        var attempt = GrammarAttemptEntity.Start("user-1", GrammarAttemptKind.StepCore, stepId, "A1", [e1], Now);
        Assert.Throws<ArgumentException>(() =>
            attempt.SubmitAnswer(e1, "{}", "InvalidStatus", 100m, 10m, 10m, null, hash, Now));
    }
}
