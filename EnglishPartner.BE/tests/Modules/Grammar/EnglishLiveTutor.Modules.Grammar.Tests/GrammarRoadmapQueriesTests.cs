using System.Text.Json;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Application.Features.Grammar.Queries;
using EnglishLiveTutor.Application.Features.Grammar.Services;
using EnglishLiveTutor.Application.Features.Identity.Services;
using EnglishLiveTutor.Domain.Grammar;
using EnglishLiveTutor.Domain.Grammar.Enums;
using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Domain.Common.Exercises;
using EnglishLiveTutor.Infrastructure.Lessons;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class GrammarRoadmapQueriesTests
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

    private static string ValidHash(int index = 1) => index.ToString("x").PadLeft(64, '0');

    private static (AppDbContext Db, FakeCurrentUser User, IGrammarLearnerContentSanitizer Sanitizer) CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .AddInterceptors(new AuditingSaveChangesInterceptor())
            .Options;

        var db = new AppDbContext(options);
        var user = new FakeCurrentUser { UserId = Guid.NewGuid().ToString() };
        var sanitizer = new GrammarLearnerContentSanitizer();

        return (db, user, sanitizer);
    }

    private static void SeedRoadmapSteps(AppDbContext db)
    {
        const string theory = "{\"objectives\":[\"Test\"],\"sections\":[{\"heading\":\"H\",\"explanationVi\":\"E\"}]}";

        // Seed A1 steps: A1-01 to A1-20 (A1-20 is review)
        for (int i = 1; i <= 20; i++)
        {
            string code = $"A1-{i:D2}";
            bool isReview = i == 20;
            db.GrammarSteps.Add(GrammarStepEntity.Create(code, "A1", i, $"Bài {code}", $"Step {code}", "Tóm tắt", theory, isLevelReview: isReview));
        }

        // Seed A2 steps: A2-01 to A2-20
        for (int i = 1; i <= 20; i++)
        {
            string code = $"A2-{i:D2}";
            bool isReview = i == 20;
            db.GrammarSteps.Add(GrammarStepEntity.Create(code, "A2", i, $"Bài {code}", $"Step {code}", "Tóm tắt", theory, isLevelReview: isReview));
        }

        db.SaveChanges();
    }

    [Fact]
    public async Task Roadmap_NewUser_OnlyA101Unlocked_CanTakeLevelTestA1Only()
    {
        var (db, user, _) = CreateTestContext();
        await using (db)
        {
            SeedRoadmapSteps(db);

            var handler = new GetGrammarRoadmapQueryHandler(db, user);
            var roadmap = await handler.HandleAsync(new GetGrammarRoadmapQuery());

            Assert.NotNull(roadmap);
            Assert.Equal(6, roadmap.Levels.Count);

            var a1 = roadmap.Levels.First(l => l.Level == "A1");
            Assert.True(a1.IsUnlocked);
            Assert.False(a1.IsPassed);
            Assert.True(a1.CanTakeLevelTest);

            var a101 = a1.Steps.First(s => s.Code == "A1-01");
            Assert.Equal("Unlocked", a101.Status);

            foreach (var step in a1.Steps.Skip(1))
            {
                Assert.Equal("Locked", step.Status);
            }

            var a2 = roadmap.Levels.First(l => l.Level == "A2");
            Assert.False(a2.IsPassed);
            Assert.False(a2.CanTakeLevelTest);
            foreach (var step in a2.Steps)
            {
                Assert.Equal("Locked", step.Status);
            }
        }
    }

    [Fact]
    public async Task Roadmap_PassA101_UnlocksA102()
    {
        var (db, user, _) = CreateTestContext();
        await using (db)
        {
            SeedRoadmapSteps(db);

            var stepA101 = await db.GrammarSteps.FirstAsync(s => s.Code == "A1-01");

            var progress = UserGrammarStepProgressEntity.Create(user.UserId!, stepA101.Id, DateTimeOffset.UtcNow);
            progress.RecordScore(85m, 70m, DateTimeOffset.UtcNow);
            db.UserGrammarStepProgress.Add(progress);
            await db.SaveChangesAsync();

            var handler = new GetGrammarRoadmapQueryHandler(db, user);
            var roadmap = await handler.HandleAsync(new GetGrammarRoadmapQuery());

            var a1 = roadmap.Levels.First(l => l.Level == "A1");
            var a101 = a1.Steps.First(s => s.Code == "A1-01");
            var a102 = a1.Steps.First(s => s.Code == "A1-02");
            var a103 = a1.Steps.First(s => s.Code == "A1-03");

            Assert.Equal("Passed", a101.Status);
            Assert.Equal(85m, a101.BestScore100);
            Assert.Equal("Unlocked", a102.Status);
            Assert.Equal("Locked", a103.Status);
        }
    }

    [Fact]
    public async Task Roadmap_PassAllA1Steps_UnlocksA201_AndCanTakeLevelTestA2()
    {
        var (db, user, _) = CreateTestContext();
        await using (db)
        {
            SeedRoadmapSteps(db);

            var a1Steps = await db.GrammarSteps.Where(s => s.CefrLevel == "A1").ToListAsync();
            foreach (var step in a1Steps)
            {
                var progress = UserGrammarStepProgressEntity.Create(user.UserId!, step.Id, DateTimeOffset.UtcNow);
                progress.RecordScore(90m, 70m, DateTimeOffset.UtcNow);
                db.UserGrammarStepProgress.Add(progress);
            }
            await db.SaveChangesAsync();

            var handler = new GetGrammarRoadmapQueryHandler(db, user);
            var roadmap = await handler.HandleAsync(new GetGrammarRoadmapQuery());

            var a1 = roadmap.Levels.First(l => l.Level == "A1");
            Assert.True(a1.IsPassed);

            var a2 = roadmap.Levels.First(l => l.Level == "A2");
            Assert.True(a2.IsUnlocked);
            Assert.True(a2.CanTakeLevelTest);

            var a201 = a2.Steps.First(s => s.Code == "A2-01");
            Assert.Equal("Unlocked", a201.Status);

            var a202 = a2.Steps.First(s => s.Code == "A2-02");
            Assert.Equal("Locked", a202.Status);
        }
    }

    [Fact]
    public async Task GetGrammarStep_LockedStep_StillReturnsTheory_AndAccurateMetadata()
    {
        var (db, user, _) = CreateTestContext();
        await using (db)
        {
            SeedRoadmapSteps(db);

            var stepA102 = await db.GrammarSteps.FirstAsync(s => s.Code == "A1-02");

            // Add official core exercise
            db.GrammarExercises.Add(GrammarExerciseEntity.CreateOfficial(
                stepA102.Id, "A1-02-E01", ExerciseType.MultipleChoice, 1, "Ex 1", "Instr", "{}", null, 10m, GradingMode.Deterministic,
                isCore: true, contentHash: ValidHash(1), now: DateTimeOffset.UtcNow));

            // Add approved community exercise
            var commEx = GrammarExerciseEntity.CreateUserOwned(
                stepA102.Id, "other-user", GrammarExerciseSource.UserAi, GrammarExerciseVisibility.Public,
                ExerciseType.MultipleChoice, 1, "Comm Ex", "Instr", "{}", null, 10m, GradingMode.Deterministic,
                contentHash: ValidHash(2), now: DateTimeOffset.UtcNow);
            commEx.Approve("admin", DateTimeOffset.UtcNow);
            db.GrammarExercises.Add(commEx);

            // Add current user's own exercise
            db.GrammarExercises.Add(GrammarExerciseEntity.CreateUserOwned(
                stepA102.Id, user.UserId!, GrammarExerciseSource.UserAi, GrammarExerciseVisibility.Private,
                ExerciseType.MultipleChoice, 1, "My Ex", "Instr", "{}", null, 10m, GradingMode.Deterministic,
                contentHash: ValidHash(3), now: DateTimeOffset.UtcNow));

            await db.SaveChangesAsync();

            var handler = new GetGrammarStepQueryHandler(db, user);
            var detail = await handler.HandleAsync(new GetGrammarStepQuery("A1-02"));

            Assert.Equal("A1-02", detail.Code);
            Assert.Equal("Locked", detail.Status); // Locked step
            Assert.Equal(JsonValueKind.Object, detail.Theory.ValueKind); // Still returns theory
            Assert.Equal("A1-01", detail.PrevCode);
            Assert.Equal("A1-03", detail.NextCode);
            Assert.Equal(1, detail.CoreExerciseCount);
            Assert.Equal(1, detail.CommunityExerciseCount);
            Assert.Equal(1, detail.MyExerciseCount);
        }
    }

    [Fact]
    public async Task GetGrammarStep_NonExistentOrUnpublished_ThrowsNotFound()
    {
        var (db, user, _) = CreateTestContext();
        await using (db)
        {
            SeedRoadmapSteps(db);

            var handler = new GetGrammarStepQueryHandler(db, user);

            await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(new GetGrammarStepQuery("Z9-99")));

            // Unpublished step
            var unpublished = GrammarStepEntity.Create("A1-99", "A1", 99, "Title", "Title", "Sum", "{}", isPublished: false);
            db.GrammarSteps.Add(unpublished);
            await db.SaveChangesAsync();

            await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(new GetGrammarStepQuery("A1-99")));
        }
    }

    [Fact]
    public async Task ListGrammarExercises_SourcesFilterCorrectly_AndSanitizesGrading()
    {
        var (db, user, sanitizer) = CreateTestContext();
        await using (db)
        {
            SeedRoadmapSteps(db);

            var step = await db.GrammarSteps.FirstAsync(s => s.Code == "A1-01");

            string contentWithAnswers = "{\"questionText\":\"What is it?\",\"correctOptionIds\":[\"opt-1\"]}";

            // 1. Core exercise
            db.GrammarExercises.Add(GrammarExerciseEntity.CreateOfficial(
                step.Id, "A1-01-E01", ExerciseType.MultipleChoice, 1, "Core Ex", "Instr",
                contentWithAnswers, "{\"explanation\":\"secret\"}", 10m, GradingMode.Deterministic,
                isCore: true, contentHash: ValidHash(1), now: DateTimeOffset.UtcNow));

            // 2. Official non-core exercise (community)
            db.GrammarExercises.Add(GrammarExerciseEntity.CreateOfficial(
                step.Id, "A1-01-E02", ExerciseType.MultipleChoice, 2, "Official NonCore", "Instr",
                contentWithAnswers, "{\"explanation\":\"secret\"}", 10m, GradingMode.Deterministic,
                isCore: false, contentHash: ValidHash(2), now: DateTimeOffset.UtcNow));

            // 3. User-owned Approved (community)
            var userApproved = GrammarExerciseEntity.CreateUserOwned(
                step.Id, "user-b", GrammarExerciseSource.UserAi, GrammarExerciseVisibility.Public,
                ExerciseType.MultipleChoice, 2, "User Approved", "Instr",
                contentWithAnswers, "{\"explanation\":\"secret\"}", 10m, GradingMode.Deterministic,
                contentHash: ValidHash(3), now: DateTimeOffset.UtcNow);
            userApproved.Approve("admin", DateTimeOffset.UtcNow);
            db.GrammarExercises.Add(userApproved);

            // 4. User-owned Pending (not in community)
            var userPending = GrammarExerciseEntity.CreateUserOwned(
                step.Id, "user-b", GrammarExerciseSource.UserAi, GrammarExerciseVisibility.Public,
                ExerciseType.MultipleChoice, 2, "User Pending", "Instr",
                contentWithAnswers, "{\"explanation\":\"secret\"}", 10m, GradingMode.Deterministic,
                contentHash: ValidHash(4), now: DateTimeOffset.UtcNow);
            db.GrammarExercises.Add(userPending);

            // 5. My own exercise (Private)
            db.GrammarExercises.Add(GrammarExerciseEntity.CreateUserOwned(
                step.Id, user.UserId!, GrammarExerciseSource.UserAi, GrammarExerciseVisibility.Private,
                ExerciseType.MultipleChoice, 3, "My Private Ex", "Instr",
                contentWithAnswers, "{\"explanation\":\"secret\"}", 10m, GradingMode.Deterministic,
                contentHash: ValidHash(5), now: DateTimeOffset.UtcNow));

            await db.SaveChangesAsync();

            var handler = new ListGrammarExercisesQueryHandler(db, new UserDirectory(db), user, sanitizer);

            // Test "core" source
            var coreResult = await handler.HandleAsync(new ListGrammarExercisesQuery("A1-01", "core"));
            Assert.Single(coreResult.Items);
            Assert.Equal("A1-01-E01", coreResult.Items[0].Key);
            // Verify content sanitized: no correctOptionIds
            Assert.False(coreResult.Items[0].Content!.Value.TryGetProperty("correctOptionIds", out _));

            // Test "community" source
            var communityResult = await handler.HandleAsync(new ListGrammarExercisesQuery("A1-01", "community"));
            Assert.Equal(2, communityResult.Items.Count);
            Assert.Contains(communityResult.Items, x => x.Title == "Official NonCore");
            Assert.Contains(communityResult.Items, x => x.Title == "User Approved");
            Assert.DoesNotContain(communityResult.Items, x => x.Title == "User Pending");

            // Test "mine" source
            var mineResult = await handler.HandleAsync(new ListGrammarExercisesQuery("A1-01", "mine"));
            Assert.Single(mineResult.Items);
            Assert.Equal("My Private Ex", mineResult.Items[0].Title);
        }
    }
}
