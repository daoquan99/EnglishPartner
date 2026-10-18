using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.Learning.Commands.InitializeDailyVocabularyPlan;
using EnglishLiveTutor.Application.Features.Learning.Commands.SubmitVocabularyReview;
using EnglishLiveTutor.Application.Features.Learning.Commands.UpdateLearningSettings;
using EnglishLiveTutor.Application.Features.Learning.Queries.GetDailyVocabularyPlan;
using EnglishLiveTutor.Application.Features.Learning.Queries.GetLearningSettings;
using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Domain.Learning;
using EnglishLiveTutor.Domain.Learning.Repositories;
using EnglishLiveTutor.Domain.Vocabulary;
using EnglishLiveTutor.Domain.Vocabulary.Entities;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Interceptors;
using EnglishLiveTutor.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class DailyVocabularyLearningTests
{
    private static (AppDbContext db, FakeTimeProvider time) CreateContext(DateTimeOffset? initialTime = null)
    {
        var time = new FakeTimeProvider(initialTime ?? new DateTimeOffset(2026, 10, 18, 8, 0, 0, TimeSpan.FromHours(7)));
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .AddInterceptors(new AuditingSaveChangesInterceptor())
            .Options;
        var db = new AppDbContext(options);
        return (db, time);
    }

    private static async Task<UserEntity> SeedUserAsync(AppDbContext db, string userId, string preferredCefr = "B1", string timeZone = "Asia/Ho_Chi_Minh")
    {
        var user = UserEntity.Create(
            username: userId,
            email: $"{userId}@example.com",
            fullName: $"User {userId}",
            passwordHash: "hash",
            preferredCefrLevel: preferredCefr,
            timeZoneId: timeZone,
            id: Guid.NewGuid());
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<VocabularyEntity> SeedVocabularyAsync(
        AppDbContext db,
        string word,
        string cefrLevel = "B1",
        int frequencyRank = 100,
        bool isPrimary = true)
    {
        var vocab = VocabularyEntity.Create(word, frequencyRank: frequencyRank, isActive: true);
        vocab.AddSense("noun", $"Definition of {word}", $"Nghĩa của {word}", cefrLevel, isPrimary: isPrimary);
        db.Vocabularies.Add(vocab);
        await db.SaveChangesAsync();
        return vocab;
    }

    [Fact]
    public void UserLearningSettings_Validation_ShouldThrowOnInvalidTargetOrLevel()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new UserLearningSettingsEntity("user-1", 0, "B1"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UserLearningSettingsEntity("user-1", 101, "B1"));
        Assert.Throws<ArgumentException>(() => new UserLearningSettingsEntity("user-1", 20, "INVALID"));
        Assert.Throws<ArgumentException>(() => new UserLearningSettingsEntity("user-1", 20, "B3"));

        var valid = new UserLearningSettingsEntity("user-1", 25, "b2");
        Assert.Equal(25, valid.DailyVocabularyTarget);
        Assert.Equal("B2", valid.CefrLevel);

        valid.Update(10, "C1");
        Assert.Equal(10, valid.DailyVocabularyTarget);
        Assert.Equal("C1", valid.CefrLevel);
    }

    [Fact]
    public void DailyVocabularyPlan_AddItem_ShouldDeduplicateByVocabularyId()
    {
        var plan = new DailyVocabularyPlanEntity("user-1", "2026-10-18", "Asia/Ho_Chi_Minh", 20, "B1");
        var vocabId = Guid.NewGuid();
        var sense1 = Guid.NewGuid();
        var sense2 = Guid.NewGuid();

        var item1 = plan.AddItem(vocabId, sense1, "New");
        var item2 = plan.AddItem(vocabId, sense2, "New");

        Assert.Same(item1, item2);
        Assert.Single(plan.Items);
        Assert.Equal(sense1, plan.Items.First().SenseId);
    }

    [Fact]
    public async Task GetLearningSettings_WhenUserHasNoSettings_ShouldReturnResolvedDefaults_WithoutWritingToDb()
    {
        var (db, time) = CreateContext();
        await SeedUserAsync(db, "learner-1", preferredCefr: "A2");

        var handler = new GetLearningSettingsQueryHandler(db, time);
        var result = await handler.HandleAsync(new GetLearningSettingsQuery("learner-1"));

        Assert.NotNull(result);
        Assert.Equal(20, result.DailyVocabularyTarget);
        Assert.Equal("A2", result.CefrLevel);
        Assert.False(result.IsPersisted);
        Assert.Equal("2026-10-18", result.EffectiveFromLearningDate);

        // Verify zero settings rows were created in DB
        Assert.False(await db.UserLearningSettings.AnyAsync());
    }

    [Fact]
    public async Task UpdateLearningSettings_ShouldPersist_AndReflectEffectiveFromTomorrow_WhenPlanExistsToday()
    {
        var (db, time) = CreateContext();
        await SeedUserAsync(db, "learner-2", preferredCefr: "B1");

        // Today already has a plan
        var existingPlan = new DailyVocabularyPlanEntity("learner-2", "2026-10-18", "Asia/Ho_Chi_Minh", 20, "B1");
        db.DailyVocabularyPlans.Add(existingPlan);
        await db.SaveChangesAsync();

        var updateHandler = new UpdateLearningSettingsCommandHandler(db, time);
        var updated = await updateHandler.HandleAsync(new UpdateLearningSettingsCommand(
            "learner-2",
            new UpdateLearningSettingsRequest(15, "B2")));

        Assert.Equal(15, updated.DailyVocabularyTarget);
        Assert.Equal("B2", updated.CefrLevel);
        Assert.True(updated.IsPersisted);
        // Tomorrow because plan exists today
        Assert.Equal("2026-10-19", updated.EffectiveFromLearningDate);

        // Snapshot of today's plan must remain unchanged!
        var storedPlan = await db.DailyVocabularyPlans.FirstAsync(p => p.Id == existingPlan.Id);
        Assert.Equal(20, storedPlan.NewVocabularyTarget);
        Assert.Equal("B1", storedPlan.TargetCefrLevel);
    }

    [Fact]
    public async Task InitializeDailyVocabularyPlan_ShouldEnforceCarryOver_Deduplicate_AndRespectTarget()
    {
        var (db, time) = CreateContext(new DateTimeOffset(2026, 10, 18, 9, 0, 0, TimeSpan.FromHours(7)));
        await SeedUserAsync(db, "user-quota", preferredCefr: "B1");

        // Seed 25 B1 words in catalog
        for (int i = 1; i <= 25; i++)
        {
            await SeedVocabularyAsync(db, $"word-b1-{i:D2}", cefrLevel: "B1", frequencyRank: i);
        }

        var initHandler = new InitializeDailyVocabularyPlanCommandHandler(db, time);

        // Day 1: Target = 20 B1
        var day1Plan = await initHandler.HandleAsync(new InitializeDailyVocabularyPlanCommand(
            "user-quota",
            new InitializeDailyPlanRequest()));

        Assert.Equal(20, day1Plan.TotalAssignedCount);
        Assert.Equal(20, day1Plan.NewCount);
        Assert.Equal(0, day1Plan.CarryOverCount);
        Assert.Equal(20, day1Plan.FirstLearningAssignedCount);

        // User reviews 12 words on Day 1
        var planRepo = new DailyVocabularyPlanRepository(db);
        var progressRepo = new UserVocabularyProgressRepository(db);
        var uow = new UnitOfWork(db);
        var reviewHandler = new SubmitVocabularyReviewCommandHandler(planRepo, progressRepo, uow, db, time);

        var first12Cards = day1Plan.Flashcards.Take(12).ToList();
        foreach (var card in first12Cards)
        {
            await reviewHandler.HandleAsync(new SubmitVocabularyReviewCommand(
                "user-quota",
                card.DailyItemId,
                new SubmitVocabularyReviewRequest("Good", card.VocabularyId, card.SenseId)));
        }

        // Advance to Day 2
        time.Advance(TimeSpan.FromDays(1)); // 2026-10-19

        // Day 2: User changes settings to B2
        var updateHandler = new UpdateLearningSettingsCommandHandler(db, time);
        await updateHandler.HandleAsync(new UpdateLearningSettingsCommand(
            "user-quota",
            new UpdateLearningSettingsRequest(20, "B2")));

        // Seed some B2 words in catalog for Day 2
        for (int i = 1; i <= 20; i++)
        {
            await SeedVocabularyAsync(db, $"word-b2-{i:D2}", cefrLevel: "B2", frequencyRank: i);
        }

        var day2Plan = await initHandler.HandleAsync(new InitializeDailyVocabularyPlanCommand(
            "user-quota",
            new InitializeDailyPlanRequest()));

        // Invariant:
        // 8 carry-over (from B1 unfinished) + 12 new B2 words = exactly 20 first learning items!
        Assert.Equal(8, day2Plan.CarryOverCount);
        Assert.Equal(12, day2Plan.NewCount);
        Assert.Equal(20, day2Plan.FirstLearningAssignedCount);
        Assert.Equal(0, day2Plan.OverflowCount);
        Assert.Equal(0, day2Plan.ShortfallCount);
        Assert.Equal("B2", day2Plan.TargetCefrLevel);

        // Verify the 8 carry-over cards retain their B1 CEFR level
        var carryOverCards = day2Plan.Flashcards.Where(c => c.ItemType == "CarryOver").ToList();
        Assert.Equal(8, carryOverCards.Count);
        Assert.All(carryOverCards, c => Assert.Equal("B1", c.CefrLevel));

        // Verify the 12 new cards are B2
        var newCards = day2Plan.Flashcards.Where(c => c.ItemType == "New").ToList();
        Assert.Equal(12, newCards.Count);
        Assert.All(newCards, c => Assert.Equal("B2", c.CefrLevel));
    }

    [Fact]
    public async Task InitializeDailyVocabularyPlan_CallingTwice_ShouldBeIdempotent_AndReturnSamePlan()
    {
        var (db, time) = CreateContext();
        await SeedUserAsync(db, "idempotent-user", preferredCefr: "B1");
        await SeedVocabularyAsync(db, "test-word-1", cefrLevel: "B1");

        var handler = new InitializeDailyVocabularyPlanCommandHandler(db, time);

        var first = await handler.HandleAsync(new InitializeDailyVocabularyPlanCommand("idempotent-user", new InitializeDailyPlanRequest()));
        var second = await handler.HandleAsync(new InitializeDailyVocabularyPlanCommand("idempotent-user", new InitializeDailyPlanRequest()));

        Assert.Equal(first.PlanId, second.PlanId);
        Assert.Equal(first.Flashcards.Count, second.Flashcards.Count);
        Assert.Single(await db.DailyVocabularyPlans.Where(p => p.UserId == "idempotent-user").ToListAsync());
    }

    [Fact]
    public async Task SubmitVocabularyReview_WithSameClientRequestId_ShouldBeIdempotent()
    {
        var (db, time) = CreateContext();
        await SeedUserAsync(db, "retry-user", preferredCefr: "B1");
        var vocab = await SeedVocabularyAsync(db, "idempotent-word", cefrLevel: "B1");

        var initHandler = new InitializeDailyVocabularyPlanCommandHandler(db, time);
        var plan = await initHandler.HandleAsync(new InitializeDailyVocabularyPlanCommand("retry-user", new InitializeDailyPlanRequest()));
        var card = plan.Flashcards.First();

        var planRepo = new DailyVocabularyPlanRepository(db);
        var progressRepo = new UserVocabularyProgressRepository(db);
        var uow = new UnitOfWork(db);
        var reviewHandler = new SubmitVocabularyReviewCommandHandler(planRepo, progressRepo, uow, db, time);

        string clientReqId = Guid.NewGuid().ToString("N");
        var req = new SubmitVocabularyReviewRequest("Good", card.VocabularyId, card.SenseId, ClientRequestId: clientReqId);

        // First review
        var firstResult = await reviewHandler.HandleAsync(new SubmitVocabularyReviewCommand("retry-user", card.DailyItemId, req));
        Assert.NotNull(firstResult);

        var progressAfterFirst = await db.UserVocabularyProgresses.FirstAsync(p => p.UserId == "retry-user" && p.SenseId == card.SenseId);
        Assert.Equal(1, progressAfterFirst.ReviewCount);

        // Retry with EXACT SAME clientRequestId (e.g. network retry)
        var secondResult = await reviewHandler.HandleAsync(new SubmitVocabularyReviewCommand("retry-user", card.DailyItemId, req));
        Assert.NotNull(secondResult);

        var progressAfterSecond = await db.UserVocabularyProgresses.FirstAsync(p => p.UserId == "retry-user" && p.SenseId == card.SenseId);
        // ReviewCount MUST remain 1, not incremented again!
        Assert.Equal(1, progressAfterSecond.ReviewCount);
        Assert.Single(await db.VocabularyReviewAttempts.Where(a => a.UserId == "retry-user").ToListAsync());
    }

    [Fact]
    public async Task Shortfall_ShouldBeReported_WhenCatalogHasInsufficientWords()
    {
        var (db, time) = CreateContext();
        await SeedUserAsync(db, "shortfall-user", preferredCefr: "C1");

        // Catalog only has 3 C1 words, target is 20
        await SeedVocabularyAsync(db, "c1-word-1", cefrLevel: "C1");
        await SeedVocabularyAsync(db, "c1-word-2", cefrLevel: "C1");
        await SeedVocabularyAsync(db, "c1-word-3", cefrLevel: "C1");

        var initHandler = new InitializeDailyVocabularyPlanCommandHandler(db, time);
        var plan = await initHandler.HandleAsync(new InitializeDailyVocabularyPlanCommand("shortfall-user", new InitializeDailyPlanRequest()));

        Assert.Equal(3, plan.TotalAssignedCount);
        Assert.Equal(3, plan.NewCount);
        Assert.Equal(17, plan.ShortfallCount); // 20 - 3 = 17 shortfall
        Assert.Equal(0, plan.OverflowCount);
    }

    [Fact]
    public async Task Overflow_ShouldBeReported_WhenCarryOverExceedsNewReducedTarget()
    {
        var (db, time) = CreateContext(new DateTimeOffset(2026, 10, 18, 8, 0, 0, TimeSpan.FromHours(7)));
        await SeedUserAsync(db, "overflow-user", preferredCefr: "A1");

        // Seed 15 words
        for (int i = 1; i <= 15; i++)
        {
            await SeedVocabularyAsync(db, $"a1-word-{i:D2}", cefrLevel: "A1", frequencyRank: i);
        }

        // Day 1: target 20, assigned 15 (shortfall 5), user reviewed 0
        var initHandler = new InitializeDailyVocabularyPlanCommandHandler(db, time);
        await initHandler.HandleAsync(new InitializeDailyVocabularyPlanCommand("overflow-user", new InitializeDailyPlanRequest()));

        // User reduces target to 10 for Day 2
        time.Advance(TimeSpan.FromDays(1));
        var updateHandler = new UpdateLearningSettingsCommandHandler(db, time);
        await updateHandler.HandleAsync(new UpdateLearningSettingsCommand(
            "overflow-user",
            new UpdateLearningSettingsRequest(10, "A1")));

        var day2Plan = await initHandler.HandleAsync(new InitializeDailyVocabularyPlanCommand("overflow-user", new InitializeDailyPlanRequest()));

        // All 15 carry-over words are kept, 0 new words added, overflowCount = 15 - 10 = 5
        Assert.Equal(15, day2Plan.CarryOverCount);
        Assert.Equal(0, day2Plan.NewCount);
        Assert.Equal(5, day2Plan.OverflowCount);
        Assert.Equal(0, day2Plan.ShortfallCount);
    }
}
