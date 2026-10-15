using System.Security.Claims;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Identity;
using EnglishLiveTutor.Application.Features.Learning.Commands.SubmitVocabularyReview;
using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Domain.Identity.Entities;
using EnglishLiveTutor.Domain.Learning;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Topics;
using EnglishLiveTutor.Domain.Vocabulary;
using EnglishLiveTutor.Domain.Vocabulary.Entities;
using EnglishLiveTutor.Infrastructure.Identity;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Interceptors;
using EnglishLiveTutor.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class RefactorRegressionTests
{
    private static AppDbContext CreateInMemoryDb(string name, TimeProvider? timeProvider = null, ICurrentUser? currentUser = null, bool useDefaultUserIfNull = true)
    {
        var clock = timeProvider ?? TimeProvider.System;
        var user = currentUser ?? (useDefaultUserIfNull ? new FakeCurrentUser() : null);
        var interceptor = new AuditingSaveChangesInterceptor(clock, user);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .AddInterceptors(interceptor)
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void ScoringRubric_UpdateRubric_ThrowsOnDuplicateCriteriaCodes_AndLeavesExistingCriteriaUnchanged()
    {
        var rubric = new ScoringRubricEntity("TEST_RUBRIC", "Initial Title");
        rubric.AddCriterion("Accuracy", "Accuracy", "Pronunciation accuracy", 50m);
        rubric.AddCriterion("Fluency", "Fluency", "Speaking fluency", 50m);

        var invalidCriteria = new List<(string Code, string Name, string Description, decimal Weight)>
        {
            ("Accuracy", "Accuracy", "Accuracy desc", 50m),
            ("Accuracy", "Duplicate Accuracy", "Duplicate code desc", 50m)
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            rubric.UpdateRubric("Updated Title", "Updated Description", invalidCriteria));

        Assert.Contains("trùng lặp", ex.Message);
        Assert.Equal(2, rubric.Criteria.Count);
        Assert.Contains(rubric.Criteria, c => c.Code == "Accuracy");
        Assert.Contains(rubric.Criteria, c => c.Code == "Fluency");
    }

    [Fact]
    public void ScoringRubric_UpdateRubric_ThrowsOnInvalidWeights_AndLeavesExistingCriteriaUnchanged()
    {
        var rubric = new ScoringRubricEntity("TEST_RUBRIC_WEIGHT", "Initial Title");
        rubric.AddCriterion("Accuracy", "Accuracy", "Pronunciation accuracy", 60m);
        rubric.AddCriterion("Fluency", "Fluency", "Speaking fluency", 40m);

        var invalidCriteria = new List<(string Code, string Name, string Description, decimal Weight)>
        {
            ("Accuracy", "Accuracy", "Accuracy desc", 40m),
            ("Fluency", "Fluency", "Fluency desc", 30m) // Total 70m != 100m
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            rubric.UpdateRubric("Updated Title", "Updated Description", invalidCriteria));

        Assert.Contains("Tổng trọng số", ex.Message);
        Assert.Equal(2, rubric.Criteria.Count);
        Assert.Equal(60m, rubric.Criteria.First(c => c.Code == "Accuracy").Weight);
    }

    [Fact]
    public void UserVocabularyProgress_ApplyReview_ThrowsOnInvalidRating()
    {
        var progress = new UserVocabularyProgressEntity(
            userId: "learner-1",
            vocabularyId: Guid.NewGuid(),
            senseId: Guid.NewGuid(),
            intervalDays: 1,
            easeFactor: 2.5m,
            learningStatus: "Learning",
            reviewCount: 0);

        var ex = Assert.Throws<ArgumentException>(() =>
            progress.ApplyReview("SuperAwesomeRating", DateTimeOffset.UtcNow));

        Assert.Contains("không hợp lệ", ex.Message);
        Assert.Equal(0, progress.ReviewCount);
    }

    [Fact]
    public void DailyVocabularyPlan_CompleteItem_ReconcilesPlanStatus()
    {
        var plan = new DailyVocabularyPlanEntity(
            userId: "learner-2",
            learningDate: "2026-10-15",
            newVocabularyTarget: 2);

        var item1 = plan.AddItem(Guid.NewGuid(), Guid.NewGuid(), "New");
        var item2 = plan.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Review");

        Assert.Equal("InProgress", plan.Status);

        var now = DateTimeOffset.UtcNow;
        plan.CompleteItem(item1.Id, now);

        Assert.Equal("Completed", item1.Status);
        Assert.Equal(now, item1.CompletedAt);
        Assert.Equal("InProgress", plan.Status);

        plan.CompleteItem(item2.Id, now);

        Assert.Equal("Completed", item2.Status);
        Assert.Equal("Completed", plan.Status);
    }

    [Fact]
    public async Task SubmitVocabularyReviewCommandHandler_ThrowsOnOwnershipMismatch()
    {
        string dbName = Guid.NewGuid().ToString("N");
        await using var db = CreateInMemoryDb(dbName);

        var plan = new DailyVocabularyPlanEntity(
            userId: "learner-alice",
            learningDate: "2026-10-15",
            newVocabularyTarget: 1);
        var item = plan.AddItem(Guid.NewGuid(), Guid.NewGuid(), "New");

        db.DailyVocabularyPlans.Add(plan);
        await db.SaveChangesAsync();

        var planRepo = new DailyVocabularyPlanRepository(db);
        var progressRepo = new UserVocabularyProgressRepository(db);
        var uow = new UnitOfWork(db);

        var handler = new SubmitVocabularyReviewCommandHandler(planRepo, progressRepo, uow, db);

        var command = new SubmitVocabularyReviewCommand(
            UserId: "learner-bob", // Bob does not own Alice's plan
            DailyItemId: item.Id,
            Request: new SubmitVocabularyReviewRequest("Good")
        );

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(command));
        Assert.Contains("Không tìm thấy mục từ vựng trong kế hoạch học tập của bạn", ex.Message);
    }

    [Fact]
    public async Task AuditingSaveChangesInterceptor_PreservesDescendantsOfSoftDeletedRoot_WithoutCancellingUnrelatedChildDeletions()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 15, 4, 0, 0, TimeSpan.Zero));
        var user = new FakeCurrentUser { UserId = "admin-tester" };
        string dbName = Guid.NewGuid().ToString("N");

        await using var db = CreateInMemoryDb(dbName, clock, user);

        var vocabA = VocabularyEntity.Create("vocabulary-a");
        var senseA1 = vocabA.AddSense("noun", "Meaning A1", "Nghĩa A1", "B1");

        var vocabB = VocabularyEntity.Create("vocabulary-b");
        var senseB1 = vocabB.AddSense("noun", "Meaning B1", "Nghĩa B1", "B1");
        var senseB2 = vocabB.AddSense("noun", "Meaning B2", "Nghĩa B2", "B2");

        db.Vocabularies.AddRange(vocabA, vocabB);
        await db.SaveChangesAsync();

        // Now in one SaveChangesAsync:
        // 1. Soft-delete vocabA
        db.Vocabularies.Remove(vocabA);
        // 2. Hard-delete senseB2 (which belongs to vocabB, NOT vocabA)
        db.VocabularySenses.Remove(senseB2);

        await db.SaveChangesAsync();

        // Check vocabA is soft-deleted
        var deletedA = await db.QueryIncludingDeleted<VocabularyEntity>()
            .Include(v => v.Senses)
            .FirstOrDefaultAsync(v => v.Id == vocabA.Id);
        Assert.NotNull(deletedA);
        Assert.True(deletedA!.IsDeleted);
        // Descendant senseA1 must be preserved as Unchanged
        Assert.Single(deletedA.Senses);

        // Check senseB2 was genuinely deleted from the database
        var remainingSensesForB = await db.VocabularySenses
            .Where(s => s.VocabularyId == vocabB.Id)
            .ToListAsync();
        Assert.Single(remainingSensesForB);
        Assert.Equal(senseB1.Id, remainingSensesForB[0].Id);
        Assert.DoesNotContain(remainingSensesForB, s => s.Id == senseB2.Id);
    }

    [Fact]
    public void HmacJwtTokenService_RejectsExpiredToken_AndRejectsInvalidSignature()
    {
        var initialTime = new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);
        var clock = new FakeTimeProvider(initialTime);

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = "SuperSecretJwtKeyThatIsLongEnoughForHmacSha256Testing12345!",
            ["Jwt:Issuer"] = "EnglishLiveTutor",
            ["Jwt:Audience"] = "EnglishLiveTutorApp",
            ["Jwt:AccessTokenMinutes"] = "15"
        }).Build();

        var tokenService = new HmacJwtTokenService(config, clock);

        var user = new UserEntity("learner@example.com", "Learner One", "hash", "salt");
        var roles = new List<string> { "Student" };
        var permissions = new List<string> { "learning.read", "learning.review" };

        var generated = tokenService.GenerateAccessToken(user, roles, permissions);
        Assert.NotNull(generated.AccessToken);

        // 1. Immediately valid
        var principal = tokenService.ValidateAccessToken(generated.AccessToken);
        Assert.NotNull(principal);
        Assert.Equal(user.Id.ToString(), principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        // 2. Tampered signature is rejected
        var tamperedToken = generated.AccessToken[..^4] + "xxxx";
        Assert.Null(tokenService.ValidateAccessToken(tamperedToken));

        // 3. Expired token is rejected
        clock.Advance(TimeSpan.FromMinutes(20)); // Passed the 15-minute expiration
        var expiredPrincipal = tokenService.ValidateAccessToken(generated.AccessToken);
        Assert.Null(expiredPrincipal);
    }

    [Fact]
    public async Task IdentityProjectionHelper_ResolveUsersRolesAndPermissionsBatchAsync_BatchesLookups()
    {
        string dbName = Guid.NewGuid().ToString("N");
        await using var db = CreateInMemoryDb(dbName);

        var u1 = new UserEntity("u1@test.com", "User One", "h", "s");
        var u2 = new UserEntity("u2@test.com", "User Two", "h", "s");
        db.Users.AddRange(u1, u2);

        var roleStudent = new RoleEntity("Student", "Student Role");
        var roleAdmin = new RoleEntity("Admin", "Admin Role");
        db.Roles.AddRange(roleStudent, roleAdmin);

        var permRead = new PermissionEntity("vocab.read", "Read vocabulary");
        var permWrite = new PermissionEntity("vocab.write", "Write vocabulary");
        db.Permissions.AddRange(permRead, permWrite);

        db.UserRoles.Add(new UserRoleEntity(u1.Id, roleStudent.Id));
        db.UserRoles.Add(new UserRoleEntity(u2.Id, roleAdmin.Id));

        db.RolePermissions.Add(new RolePermissionEntity(roleStudent.Id, permRead.Id));
        db.RolePermissions.Add(new RolePermissionEntity(roleAdmin.Id, permRead.Id));
        db.RolePermissions.Add(new RolePermissionEntity(roleAdmin.Id, permWrite.Id));

        await db.SaveChangesAsync();

        var resultMap = await IdentityProjectionHelper.ResolveUsersRolesAndPermissionsBatchAsync(
            db, new List<Guid> { u1.Id, u2.Id });

        Assert.Equal(2, resultMap.Count);
        Assert.Contains("STUDENT", resultMap[u1.Id].RoleCodes);
        Assert.Contains("ADMIN", resultMap[u2.Id].RoleCodes);

        Assert.Single(resultMap[u1.Id].PermissionCodes);
        Assert.Equal(2, resultMap[u2.Id].PermissionCodes.Count);
    }

    [Fact]
    public async Task AuditingSaveChangesInterceptor_WhenCurrentUserIsNull_ShouldFallBackToSystemString()
    {
        string dbName = Guid.NewGuid().ToString("N");
        // pass currentUser: null with useDefaultUserIfNull: false
        await using var db = CreateInMemoryDb(dbName, currentUser: null, useDefaultUserIfNull: false);

        var topic = new TopicEntity("T_SYS", "SystemCreatedTopic", "Chủ đề hệ thống", "General");
        db.Topics.Add(topic);
        await db.SaveChangesAsync();

        var reloaded = await db.Topics.FindAsync(topic.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("system", reloaded.CreatedBy);
    }

    [Fact]
    public async Task AuditingSaveChangesInterceptor_WhenCurrentUserHasUserId_ShouldUseCurrentUserUserId()
    {
        string dbName = Guid.NewGuid().ToString("N");
        var customUser = new FakeCurrentUser { UserId = "learner-999" };
        await using var db = CreateInMemoryDb(dbName, currentUser: customUser);

        var topic = new TopicEntity("T_LRN", "LearnerCreatedTopic", "Chủ đề học viên", "General");
        db.Topics.Add(topic);
        await db.SaveChangesAsync();

        var reloaded = await db.Topics.FindAsync(topic.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("learner-999", reloaded.CreatedBy);
    }

    [Fact]
    public async Task SubmitVocabularyReviewCommandHandler_ReviewBySenseId_SucceedsAndCompletesPlanItem()
    {
        string dbName = Guid.NewGuid().ToString("N");
        await using var db = CreateInMemoryDb(dbName);

        var vocab = new VocabularyEntity("eloquent", "adj", "B2");
        var sense = vocab.AddSense("fluent or persuasive in speaking or writing", "lưu loát", "B2", isPrimary: true);
        db.Vocabularies.Add(vocab);

        var plan = new DailyVocabularyPlanEntity(
            userId: "learner-1",
            learningDate: "2026-10-15",
            newVocabularyTarget: 1);
        var item = plan.AddItem(vocab.Id, sense.Id, "New");
        db.DailyVocabularyPlans.Add(plan);
        await db.SaveChangesAsync();

        var planRepo = new DailyVocabularyPlanRepository(db);
        var progressRepo = new UserVocabularyProgressRepository(db);
        var uow = new UnitOfWork(db);
        var handler = new SubmitVocabularyReviewCommandHandler(planRepo, progressRepo, uow, db);

        // Call by SenseId and VocabularyId without DailyItemId (matching FE call)
        var command = new SubmitVocabularyReviewCommand(
            UserId: "learner-1",
            DailyItemId: null,
            Request: new SubmitVocabularyReviewRequest(
                Rating: "Good",
                VocabularyId: vocab.Id,
                SenseId: sense.Id,
                ResponseTimeMs: 2500)
        );

        var result = await handler.HandleAsync(command);

        Assert.NotNull(result);
        Assert.Equal(vocab.Id, result.VocabularyId);
        Assert.Equal(sense.Id, result.SenseId);
        Assert.Equal("Completed", result.ItemStatus);

        var progress = await progressRepo.GetByUserAndSenseAsync("learner-1", sense.Id);
        Assert.NotNull(progress);
        Assert.Equal(1, progress.ReviewCount);

        var reloadedPlan = await planRepo.GetByIdWithItemsAsync(plan.Id);
        Assert.NotNull(reloadedPlan);
        Assert.Equal("Completed", reloadedPlan.Items.First().Status);
    }
}
