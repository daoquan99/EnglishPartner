using EnglishLiveTutor.Domain.Common.Abstractions;
using EnglishLiveTutor.Domain.Common.Primitives;
using EnglishLiveTutor.Domain.Conversations;
using EnglishLiveTutor.Domain.Learning;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Vocabulary;
using EnglishLiveTutor.Domain.Vocabulary.Entities;
using EnglishLiveTutor.Domain.Vocabulary.Enums;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class DomainLogicTests
{
    [Fact]
    public void NormalizeWord_ShouldLowercaseAndCollapseWhitespace()
    {
        string raw = "  Unprecedented   Word  ";
        string normalized = VocabularyEntity.NormalizeWord(raw);

        Assert.Equal("unprecedented word", normalized);
    }

    [Fact]
    public void Entity_ShouldImplementIAuditable_AndSupportIdentityEquality()
    {
        var id = Guid.NewGuid();
        var vocab1 = VocabularyEntity.Create("resilient", id: id);
        var vocab2 = VocabularyEntity.Create("scalable", id: id);

        Assert.IsAssignableFrom<IAuditable>(vocab1);
        Assert.IsAssignableFrom<Entity<Guid>>(vocab1);
        Assert.Equal(vocab1, vocab2);
        Assert.True(vocab1 == vocab2);

        var createdTime = new DateTimeOffset(2026, 10, 15, 0, 0, 0, TimeSpan.Zero);
        var updatedTime = createdTime.AddMinutes(10);

        vocab1.SetCreationAudit(createdTime, "tester-1");
        vocab1.SetModificationAudit(updatedTime, "tester-2");

        Assert.Equal(createdTime, vocab1.CreatedAtUtc);
        Assert.Equal("tester-1", vocab1.CreatedBy);
        Assert.Equal(updatedTime, vocab1.UpdatedAtUtc);
        Assert.Equal("tester-2", vocab1.UpdatedBy);
    }

    [Fact]
    public void AggregateRoot_SoftDelete_ShouldBeIdempotent_AndRestoreShouldClearDeletionState()
    {
        var vocab = VocabularyEntity.Create("collaborate");
        Assert.IsAssignableFrom<ISoftDeletable>(vocab);
        Assert.IsAssignableFrom<AggregateRoot<Guid>>(vocab);
        Assert.False(vocab.IsDeleted);
        Assert.True(vocab.IsActive);

        var firstDeleteAt = new DateTimeOffset(2026, 10, 15, 1, 0, 0, TimeSpan.Zero);
        var secondDeleteAt = firstDeleteAt.AddHours(2);

        vocab.SoftDelete(firstDeleteAt, "admin-1");

        Assert.True(vocab.IsDeleted);
        Assert.Equal(firstDeleteAt, vocab.DeletedAtUtc);
        Assert.Equal("admin-1", vocab.DeletedBy);
        Assert.True(vocab.IsActive);

        // Idempotent second call must not overwrite original deletion timestamp or actor
        vocab.SoftDelete(secondDeleteAt, "admin-2");
        Assert.True(vocab.IsDeleted);
        Assert.Equal(firstDeleteAt, vocab.DeletedAtUtc);
        Assert.Equal("admin-1", vocab.DeletedBy);

        // Restore must clear soft-delete state and be idempotent
        vocab.Restore();
        Assert.False(vocab.IsDeleted);
        Assert.Null(vocab.DeletedAtUtc);
        Assert.Null(vocab.DeletedBy);

        vocab.Restore();
        Assert.False(vocab.IsDeleted);
    }

    [Fact]
    public void VocabularyAggregate_ShouldManageChildEntitiesAndEnforceInvariants()
    {
        var vocab = VocabularyEntity.Create(
            word: "  Bank  ",
            languageCode: "EN",
            sourceType: nameof(VocabularySourceType.Seeded),
            frequencyRank: 410,
            topicCategory: "Finance & Geography");

        Assert.Equal("Bank", vocab.Word);
        Assert.Equal("bank", vocab.NormalizedWord);
        Assert.Equal("en", vocab.LanguageCode);

        var pronUk = vocab.AddPronunciation("uk", "/bæŋk/", syllableCount: 1, stressPattern: "bank (O)");
        var wf = vocab.AddWordFamily("banker", "noun");

        var sense1 = vocab.AddSense(
            partOfSpeech: "Noun",
            definitionEn: "Financial institution",
            meaningVi: "Ngân hàng",
            cefrLevel: "a2",
            isPrimary: true);

        var sense2 = vocab.AddSense(
            partOfSpeech: "noun",
            definitionEn: "Land alongside a river",
            meaningVi: "Bờ sông",
            cefrLevel: "b1",
            isPrimary: false);

        sense1.AddRelation("Synonym", "lender");
        sense1.AddCollocation("open a bank account", "mở tài khoản ngân hàng");
        sense1.AddExample("I went to the bank.", "Tôi đã đến ngân hàng.");
        sense1.AddGrammarPattern("withdraw money from a bank", "Rút tiền từ ngân hàng", "She withdrew cash from the bank.");

        Assert.Single(vocab.Pronunciations);
        Assert.Equal("UK", pronUk.Accent);
        Assert.Single(vocab.WordFamilies);
        Assert.Equal("banker", wf.RelatedWord);
        Assert.Equal(2, vocab.Senses.Count);
        Assert.True(sense1.IsPrimary);
        Assert.False(sense2.IsPrimary);
        Assert.Equal(1, sense1.DisplayOrder);
        Assert.Equal(2, sense2.DisplayOrder);
        Assert.Single(sense1.Relations);
        Assert.Single(sense1.Collocations);
        Assert.Single(sense1.Examples);
        Assert.Single(sense1.GrammarPatterns);

        vocab.UpdateDefinition(sense1.Id, "Updated English definition", "Ngân hàng thương mại", "Updated description");
        Assert.Equal("Updated English definition", sense1.DefinitionEn);
        Assert.Equal("Ngân hàng thương mại", sense1.MeaningVi);
        Assert.Equal("Updated description", sense1.DescriptionEn);

        vocab.Deactivate();
        Assert.False(vocab.IsActive);
        Assert.False(vocab.IsDeleted);
        vocab.Activate();
        Assert.True(vocab.IsActive);
    }

    [Fact]
    public void ApplyReview_AgainRating_ShouldResetIntervalAndSetNextReviewIn4Hours()
    {
        var progress = new UserVocabularyProgressEntity(
            userId: "default-learner",
            vocabularyId: Guid.NewGuid(),
            senseId: Guid.NewGuid(),
            masteryLevel: 50,
            intervalDays: 12,
            easeFactor: 2.5m,
            learningStatus: "Review",
            reviewCount: 3);

        var now = DateTimeOffset.UtcNow;
        string result = progress.ApplyReview("Again", now);

        Assert.Equal("Again", result);
        Assert.Equal(4, progress.ReviewCount);
        Assert.Equal(1, progress.IncorrectCount);
        Assert.Equal(1, progress.IntervalDays);
        Assert.Equal(2.30m, progress.EaseFactor);
        Assert.Equal(35, progress.MasteryLevel);
        Assert.Equal("Learning", progress.LearningStatus);
        Assert.Equal(now.AddHours(4), progress.NextReviewAt);
    }

    [Fact]
    public void ApplyReview_EasyRating_AdvancesIntervalAndMastery()
    {
        var progress = new UserVocabularyProgressEntity(
            userId: "default-learner",
            vocabularyId: Guid.NewGuid(),
            senseId: Guid.NewGuid(),
            masteryLevel: 60,
            intervalDays: 0,
            easeFactor: 2.50m,
            learningStatus: "Learning",
            reviewCount: 0);

        var now = DateTimeOffset.UtcNow;
        string result = progress.ApplyReview("Easy", now);

        Assert.Equal("Easy", result);
        Assert.Equal(1, progress.ReviewCount);
        Assert.Equal(1, progress.CorrectCount);
        Assert.Equal(5, progress.IntervalDays);
        Assert.Equal(2.65m, progress.EaseFactor);
        Assert.Equal(90, progress.MasteryLevel);
        Assert.Equal("Mastered", progress.LearningStatus);
        Assert.Equal(now.AddDays(5), progress.NextReviewAt);
    }

    [Fact]
    public void ScoringRubric_ValidateWeights_ThrowsWhenTotalIsNot100()
    {
        var rubric = new ScoringRubricEntity("TEST_RUBRIC", "Test Rubric");
        rubric.AddCriterion("Accuracy", "Accuracy", "Desc", 50m);
        rubric.AddCriterion("Clarity", "Clarity", "Desc", 40m); // Total 90m

        var ex = Assert.Throws<InvalidOperationException>(() => rubric.ValidateWeights());
        Assert.Contains("Tổng trọng số", ex.Message);
    }

    [Fact]
    public void ScoringRubric_ValidateWeights_PassesWhenTotalIs100()
    {
        var rubric = new ScoringRubricEntity("VALID_RUBRIC", "Valid Rubric");
        rubric.AddCriterion("Accuracy", "Accuracy", "Desc", 40m);
        rubric.AddCriterion("Clarity", "Clarity", "Desc", 30m);
        rubric.AddCriterion("Fluency", "Fluency", "Desc", 30m);

        rubric.ValidateWeights();
    }

    [Fact]
    public void PronunciationAssessment_CalculateWeightedScore_RoundsAccurately()
    {
        decimal rawScore = 85.5m;
        decimal weight = 40.0m;

        decimal weighted = PronunciationAssessmentEntity.CalculateWeightedScore(rawScore, weight);

        Assert.Equal(34.20m, weighted);
    }
}
