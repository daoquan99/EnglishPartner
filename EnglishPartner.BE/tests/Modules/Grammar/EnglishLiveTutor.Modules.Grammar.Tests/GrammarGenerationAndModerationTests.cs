using System.Text.Json;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.Grammar.Commands.Admin.ApproveGrammarExercise;
using EnglishLiveTutor.Application.Features.Grammar.Commands.Admin.DraftGrammarTheoryWithAi;
using EnglishLiveTutor.Application.Features.Grammar.Commands.Admin.GenerateOfficialGrammarExercises;
using EnglishLiveTutor.Application.Features.Grammar.Commands.Admin.PromoteGrammarExercise;
using EnglishLiveTutor.Application.Features.Grammar.Commands.Admin.RejectGrammarExercise;
using EnglishLiveTutor.Application.Features.Grammar.Commands.ChangeMyGrammarExerciseVisibility;
using EnglishLiveTutor.Application.Features.Grammar.Commands.DeleteMyGrammarExercise;
using EnglishLiveTutor.Application.Features.Grammar.Commands.GenerateGrammarExercises;
using EnglishLiveTutor.Application.Features.Grammar.Commands.ImportGrammarExercisesJson;
using EnglishLiveTutor.Application.Features.Grammar.Commands.UpdateMyGrammarExercise;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Application.Features.Grammar.Ports;
using EnglishLiveTutor.Application.Features.Grammar.Queries;
using EnglishLiveTutor.Application.Features.Grammar.Queries.Admin.ListGrammarModerationQueue;
using EnglishLiveTutor.Application.Features.Grammar.Queries.ListMyGrammarExercises;
using EnglishLiveTutor.Application.Features.Grammar.Rules;
using EnglishLiveTutor.Application.Features.Grammar.Services;
using EnglishLiveTutor.Application.Features.Identity.Services;
using EnglishLiveTutor.Domain.Grammar;
using EnglishLiveTutor.Domain.Grammar.Enums;
using EnglishLiveTutor.Domain.Common.Exercises;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class GrammarGenerationAndModerationTests
{
    private static DbContextOptions<AppDbContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
    }

    private sealed class CountingUnitOfWork : IUnitOfWork
    {
        private readonly IUnitOfWork _inner;
        public int CommitCount { get; private set; }

        public CountingUnitOfWork(IUnitOfWork inner)
        {
            _inner = inner;
        }

        public async Task<int> CommitChangesAsync(CancellationToken ct = default)
        {
            CommitCount++;
            return await _inner.CommitChangesAsync(ct);
        }

        public Task<TResponse> ExecuteInTransactionAsync<TResponse>(
            Func<CancellationToken, Task<TResponse>> operation,
            CancellationToken ct = default)
        {
            return _inner.ExecuteInTransactionAsync(operation, ct);
        }
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public string? UserId { get; set; } = "user-a";
        public string UserIdOrSystem => UserId ?? "system";
        public IReadOnlyList<string> Roles { get; set; } = new List<string> { "LEARNER" };
        public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);
        public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
        public bool HasPermission(string permission) => true;
    }

    private sealed class FakeGrammarExerciseAiGenerator : IGrammarExerciseAiGenerator
    {
        public Func<string, GrammarAiExecutionConfig, Task<string>>? OnGenerate { get; set; }
        public Action? OnGenerateInvoked { get; set; }

        public Task<string> GenerateRawAsync(string prompt, GrammarAiExecutionConfig config, CancellationToken ct = default)
        {
            OnGenerateInvoked?.Invoke();
            if (OnGenerate != null)
            {
                return OnGenerate(prompt, config);
            }

            string defaultJson = @"{
  ""exercises"": [
    {
      ""type"": ""MultipleChoice"",
      ""difficulty"": 1,
      ""focus"": ""to be affirmative"",
      ""title"": ""To be MC 1"",
      ""instructions"": ""Choose the correct answer"",
      ""content"": {
        ""question"": ""She ____ a nurse."",
        ""selectionMode"": ""Single"",
        ""options"": [
          { ""id"": ""opt-a"", ""text"": ""is"" },
          { ""id"": ""opt-b"", ""text"": ""am"" }
        ]
      },
      ""grading"": {
        ""correctOptionIds"": [""opt-a""],
        ""explanation"": ""She đi với is""
      }
    }
  ]
}";
            return Task.FromResult(defaultJson);
        }
    }

    private static async Task<GrammarStepEntity> SeedStepAsync(AppDbContext db, string code = "A1-03", string level = "A1", int order = 3)
    {
        string theory = @"{
  ""objectives"": [""Học to be khẳng định""],
  ""sections"": [
    {
      ""heading"": ""Động từ to be"",
      ""explanationVi"": ""Giải thích"",
      ""formulas"": [{ ""label"": ""Khẳng định"", ""pattern"": ""S + am/is/are"" }],
      ""examples"": [{ ""en"": ""I am a student."", ""vi"": ""Tôi là sinh viên."" }]
    }
  ],
  ""commonMistakes"": [
    { ""wrong"": ""I hungry"", ""right"": ""I am hungry"", ""explanationVi"": ""Thiếu to be"" }
  ],
  ""summary"": [""Tổng kết""]
}";

        var step = GrammarStepEntity.Create(
            code: code,
            cefrLevel: level,
            order: order,
            titleVi: "To be khẳng định",
            titleEn: "To be Affirmative",
            summaryVi: "Học cách dùng am/is/are",
            theoryJson: theory,
            theoryVersion: 1,
            isLevelReview: false,
            isPublished: true);

        db.GrammarSteps.Add(step);
        await db.SaveChangesAsync();
        return step;
    }

    #region 1. Provider Called Outside Transaction & Failure Tests

    [Fact]
    public async Task GenerateExercises_ProviderCalledBeforeEntitiesTracked_AndCommitsExactlyOnce()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var countingUow = new CountingUnitOfWork(uow);
        var currentUser = new TestCurrentUser { UserId = "user-a" };
        var step = await SeedStepAsync(db);

        bool wasCalledBeforeWrites = false;
        var fakeAi = new FakeGrammarExerciseAiGenerator();
        fakeAi.OnGenerateInvoked = () =>
        {
            wasCalledBeforeWrites = !db.ChangeTracker.Entries<GrammarExerciseEntity>().Any();
        };

        var contextLoader = new GrammarExercisePromptContextLoader(db);
        var promptBuilder = new GrammarExercisePromptBuilder();
        var processor = new GrammarExerciseOutputProcessor();

        var handler = new GenerateGrammarExercisesCommandHandler(
            db, countingUow, currentUser, TimeProvider.System,
            contextLoader, promptBuilder, fakeAi, processor);

        var cmd = new GenerateGrammarExercisesCommand(
            StepCode: step.Code,
            Request: new GenerateGrammarExercisesRequest(
                Items: new List<GrammarExercisePromptRequestedItem> { new(ExerciseType.MultipleChoice, 1, 1) },
                Visibility: GrammarExerciseVisibility.Private
            )
        );

        var result = await handler.HandleAsync(cmd);

        Assert.True(wasCalledBeforeWrites, "AI provider must be called before any entity is tracked in ChangeTracker.");
        Assert.Equal(1, countingUow.CommitCount);
        Assert.Single(result.Created);
        Assert.Empty(result.Rejected);
        Assert.Equal(GrammarExerciseVisibility.Private, result.Created[0].Visibility);
        Assert.Equal(GrammarModerationStatus.None, result.Created[0].Moderation);
    }

    [Fact]
    public async Task GenerateExercises_WhenProviderThrows_ZeroCommits_AndNoEntitiesSaved()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var countingUow = new CountingUnitOfWork(uow);
        var currentUser = new TestCurrentUser { UserId = "user-a" };
        var step = await SeedStepAsync(db);

        var failingAi = new FakeGrammarExerciseAiGenerator
        {
            OnGenerate = (_, _) => throw new HttpRequestException("Gemini service unavailable")
        };

        var contextLoader = new GrammarExercisePromptContextLoader(db);
        var promptBuilder = new GrammarExercisePromptBuilder();
        var processor = new GrammarExerciseOutputProcessor();

        var handler = new GenerateGrammarExercisesCommandHandler(
            db, countingUow, currentUser, TimeProvider.System,
            contextLoader, promptBuilder, failingAi, processor);

        var cmd = new GenerateGrammarExercisesCommand(
            StepCode: step.Code,
            Request: new GenerateGrammarExercisesRequest(
                Items: new List<GrammarExercisePromptRequestedItem> { new(ExerciseType.MultipleChoice, 1, 1) },
                Visibility: GrammarExerciseVisibility.Private
            )
        );

        await Assert.ThrowsAsync<HttpRequestException>(() => handler.HandleAsync(cmd));
        Assert.Equal(0, countingUow.CommitCount);

        int count = await db.GrammarExercises.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GenerateExercises_WhenZeroValidExercises_Throws422_AndZeroCommits()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var countingUow = new CountingUnitOfWork(uow);
        var currentUser = new TestCurrentUser { UserId = "user-a" };
        var step = await SeedStepAsync(db);

        // AI returns exercises with completely invalid type
        var invalidAi = new FakeGrammarExerciseAiGenerator
        {
            OnGenerate = (_, _) => Task.FromResult(@"{ ""exercises"": [ { ""type"": ""UnknownType"" } ] }")
        };

        var contextLoader = new GrammarExercisePromptContextLoader(db);
        var promptBuilder = new GrammarExercisePromptBuilder();
        var processor = new GrammarExerciseOutputProcessor();

        var handler = new GenerateGrammarExercisesCommandHandler(
            db, countingUow, currentUser, TimeProvider.System,
            contextLoader, promptBuilder, invalidAi, processor);

        var cmd = new GenerateGrammarExercisesCommand(
            StepCode: step.Code,
            Request: new GenerateGrammarExercisesRequest(
                Items: new List<GrammarExercisePromptRequestedItem> { new(ExerciseType.MultipleChoice, 1, 1) }
            )
        );

        var ex = await Assert.ThrowsAsync<UnprocessableEntityException>(() => handler.HandleAsync(cmd));
        Assert.Equal("grammar_generation_no_valid_exercises", ex.Message);
        Assert.NotNull(ex.Extensions);
        Assert.True(ex.Extensions.ContainsKey("rejected"));
        Assert.Equal(0, countingUow.CommitCount);
    }

    #endregion

    #region 2. Quota & Limits Tests

    [Fact]
    public async Task GenerateExercises_ExceedingDailyQuota_Throws429TooManyRequests()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var currentUser = new TestCurrentUser { UserId = "quota-user" };
        var step = await SeedStepAsync(db);

        var now = DateTimeOffset.UtcNow;
        // Seed 40 exercises in past 24h
        for (int i = 0; i < 40; i++)
        {
            var exercise = GrammarExerciseEntity.CreateUserOwned(
                stepId: step.Id,
                ownerUserId: "quota-user",
                source: GrammarExerciseSource.UserAi,
                visibility: GrammarExerciseVisibility.Private,
                type: ExerciseType.MultipleChoice,
                difficulty: 1,
                title: $"Title {i}",
                instructions: "Inst",
                contentJson: "{}",
                gradingDefinitionJson: "{}",
                maxPoints: 10m,
                gradingMode: GradingMode.Deterministic,
                contentHash: new string((char)('0' + (i % 10)), 64),
                now: now.AddHours(-2));
            db.GrammarExercises.Add(exercise);
        }
        await db.SaveChangesAsync();

        var fakeAi = new FakeGrammarExerciseAiGenerator();
        var contextLoader = new GrammarExercisePromptContextLoader(db);
        var promptBuilder = new GrammarExercisePromptBuilder();
        var processor = new GrammarExerciseOutputProcessor();

        var handler = new GenerateGrammarExercisesCommandHandler(
            db, uow, currentUser, TimeProvider.System,
            contextLoader, promptBuilder, fakeAi, processor);

        // Request 1 more -> total 41 -> throws 429
        var cmd = new GenerateGrammarExercisesCommand(
            StepCode: step.Code,
            Request: new GenerateGrammarExercisesRequest(
                Items: new List<GrammarExercisePromptRequestedItem> { new(ExerciseType.MultipleChoice, 1, 1) }
            )
        );

        var ex = await Assert.ThrowsAsync<TooManyRequestsException>(() => handler.HandleAsync(cmd));
        Assert.Equal("grammar_daily_quota_exceeded", ex.Message);
    }

    [Fact]
    public async Task GenerateExercises_PendingLimitExceeded_Throws409ForPublic_AllowsPrivate()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var currentUser = new TestCurrentUser { UserId = "pending-user" };
        var step = await SeedStepAsync(db);

        var now = DateTimeOffset.UtcNow;
        // Seed 30 Pending exercises
        for (int i = 0; i < 30; i++)
        {
            var exercise = GrammarExerciseEntity.CreateUserOwned(
                stepId: step.Id,
                ownerUserId: "pending-user",
                source: GrammarExerciseSource.UserAi,
                visibility: GrammarExerciseVisibility.Public, // Sets Pending
                type: ExerciseType.MultipleChoice,
                difficulty: 1,
                title: $"Pending {i}",
                instructions: "Inst",
                contentJson: "{}",
                gradingDefinitionJson: "{}",
                maxPoints: 10m,
                gradingMode: GradingMode.Deterministic,
                contentHash: new string((char)('0' + (i % 10)), 64),
                now: now.AddDays(-2)); // Created older than 24h so not hitting daily quota
            db.GrammarExercises.Add(exercise);
        }
        await db.SaveChangesAsync();

        var fakeAi = new FakeGrammarExerciseAiGenerator();
        var contextLoader = new GrammarExercisePromptContextLoader(db);
        var promptBuilder = new GrammarExercisePromptBuilder();
        var processor = new GrammarExerciseOutputProcessor();

        var handler = new GenerateGrammarExercisesCommandHandler(
            db, uow, currentUser, TimeProvider.System,
            contextLoader, promptBuilder, fakeAi, processor);

        // 1. Requesting Public with 30 pending -> Conflict 409
        var publicCmd = new GenerateGrammarExercisesCommand(
            StepCode: step.Code,
            Request: new GenerateGrammarExercisesRequest(
                Items: new List<GrammarExercisePromptRequestedItem> { new(ExerciseType.MultipleChoice, 1, 1) },
                Visibility: GrammarExerciseVisibility.Public
            )
        );

        var ex = await Assert.ThrowsAsync<ConflictException>(() => handler.HandleAsync(publicCmd));
        Assert.Equal("grammar_pending_limit", ex.Message);

        // 2. Requesting Private with 30 pending -> Success!
        var privateCmd = new GenerateGrammarExercisesCommand(
            StepCode: step.Code,
            Request: new GenerateGrammarExercisesRequest(
                Items: new List<GrammarExercisePromptRequestedItem> { new(ExerciseType.MultipleChoice, 1, 1) },
                Visibility: GrammarExerciseVisibility.Private
            )
        );

        var result = await handler.HandleAsync(privateCmd);
        Assert.Single(result.Created);
        Assert.Equal(GrammarExerciseVisibility.Private, result.Created[0].Visibility);
        Assert.Equal(GrammarModerationStatus.None, result.Created[0].Moderation);
    }

    #endregion

    #region 3. My Exercises & Security (Ownership) Tests

    [Fact]
    public async Task Owner_EditsApprovedPublicExercise_RevertsToPending_AndDisappearsFromCommunityQuery()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var currentUser = new TestCurrentUser { UserId = "user-a" };
        var step = await SeedStepAsync(db);

        var now = DateTimeOffset.UtcNow;
        string contentJson = @"{""question"":""I ____ here."",""selectionMode"":""Single"",""options"":[{""id"":""opt-a"",""text"":""am""},{""id"":""opt-b"",""text"":""is""}]}";
        string gradingJson = @"{""correctOptionIds"":[""opt-a""],""explanation"":""I am""}";
        string hash = GrammarContentHasher.ComputeHash(ExerciseType.MultipleChoice, contentJson, gradingJson);

        var exercise = GrammarExerciseEntity.CreateUserOwned(
            stepId: step.Id,
            ownerUserId: "user-a",
            source: GrammarExerciseSource.UserAi,
            visibility: GrammarExerciseVisibility.Public,
            type: ExerciseType.MultipleChoice,
            difficulty: 1,
            title: "Original MC",
            instructions: "Inst",
            contentJson: contentJson,
            gradingDefinitionJson: gradingJson,
            maxPoints: 10m,
            gradingMode: GradingMode.Deterministic,
            contentHash: hash,
            now: now);

        exercise.Approve("admin", now);
        db.GrammarExercises.Add(exercise);
        await db.SaveChangesAsync();

        // Check user-b sees it in community query before edit
        var sanitizer = new GrammarLearnerContentSanitizer();
        var listHandler = new ListGrammarExercisesQueryHandler(db, new UserDirectory(db), new TestCurrentUser { UserId = "user-b" }, sanitizer);
        var communityResult = await listHandler.HandleAsync(new ListGrammarExercisesQuery(step.Code, Source: "community"));
        Assert.Single(communityResult.Items);

        // Now user-a updates content
        var updateHandler = new UpdateMyGrammarExerciseCommandHandler(db, uow, currentUser, TimeProvider.System);
        string newContent = @"{""question"":""I ____ updated."",""selectionMode"":""Single"",""options"":[{""id"":""opt-a"",""text"":""am""},{""id"":""opt-b"",""text"":""are""}]}";
        using var newContentDoc = JsonDocument.Parse(newContent);
        using var newGradingDoc = JsonDocument.Parse(gradingJson);

        var updated = await updateHandler.HandleAsync(new UpdateMyGrammarExerciseCommand(
            Id: exercise.Id,
            Request: new UpdateMyGrammarExerciseRequest(
                Title: "Updated MC",
                Instructions: "Inst updated",
                Difficulty: 2,
                Content: newContentDoc.RootElement.Clone(),
                Grading: newGradingDoc.RootElement.Clone()
            )
        ));

        // Exercise status must revert to Pending
        Assert.Equal(GrammarModerationStatus.Pending, updated.Moderation);

        // User-b querying community must now see 0 exercises!
        communityResult = await listHandler.HandleAsync(new ListGrammarExercisesQuery(step.Code, Source: "community"));
        Assert.Empty(communityResult.Items);
    }

    [Fact]
    public async Task CrossUser_UserBCannotEdit_Delete_OrChangeVisibility_OfUserA_Returns404()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var step = await SeedStepAsync(db);

        var now = DateTimeOffset.UtcNow;
        string contentJson = @"{""question"":""Q"",""selectionMode"":""Single"",""options"":[{""id"":""opt-a"",""text"":""a""}]}";
        string hash = GrammarContentHasher.ComputeHash(ExerciseType.MultipleChoice, contentJson, "{}");

        var exercise = GrammarExerciseEntity.CreateUserOwned(
            stepId: step.Id,
            ownerUserId: "user-a",
            source: GrammarExerciseSource.UserAi,
            visibility: GrammarExerciseVisibility.Private,
            type: ExerciseType.MultipleChoice,
            difficulty: 1,
            title: "User A Exercise",
            instructions: "Inst",
            contentJson: contentJson,
            gradingDefinitionJson: "{}",
            maxPoints: 10m,
            gradingMode: GradingMode.Deterministic,
            contentHash: hash,
            now: now);

        db.GrammarExercises.Add(exercise);
        await db.SaveChangesAsync();

        // User B tries to modify
        var userBCurrent = new TestCurrentUser { UserId = "user-b" };

        var updateHandler = new UpdateMyGrammarExerciseCommandHandler(db, uow, userBCurrent, TimeProvider.System);
        using var doc = JsonDocument.Parse(contentJson);
        await Assert.ThrowsAsync<NotFoundException>(() => updateHandler.HandleAsync(new UpdateMyGrammarExerciseCommand(
            Id: exercise.Id,
            Request: new UpdateMyGrammarExerciseRequest("Hacked Title", "Hacked", 1, doc.RootElement.Clone(), null)
        )));

        var visHandler = new ChangeMyGrammarExerciseVisibilityCommandHandler(db, uow, userBCurrent, TimeProvider.System);
        await Assert.ThrowsAsync<NotFoundException>(() => visHandler.HandleAsync(new ChangeMyGrammarExerciseVisibilityCommand(
            Id: exercise.Id,
            Request: new ChangeMyGrammarExerciseVisibilityRequest(GrammarExerciseVisibility.Public)
        )));

        var delHandler = new DeleteMyGrammarExerciseCommandHandler(db, uow, userBCurrent, TimeProvider.System);
        await Assert.ThrowsAsync<NotFoundException>(() => delHandler.HandleAsync(new DeleteMyGrammarExerciseCommand(exercise.Id)));
    }

    [Fact]
    public async Task DeleteMyExercise_PromotedExercise_Returns404()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var step = await SeedStepAsync(db);

        var now = DateTimeOffset.UtcNow;
        string content = @"{""question"":""Q"",""selectionMode"":""Single"",""options"":[{""id"":""opt-a"",""text"":""a""}]}";
        string hash = GrammarContentHasher.ComputeHash(ExerciseType.MultipleChoice, content, "{}");

        var exercise = GrammarExerciseEntity.CreateUserOwned(
            stepId: step.Id,
            ownerUserId: "user-a",
            source: GrammarExerciseSource.UserAi,
            visibility: GrammarExerciseVisibility.Public,
            type: ExerciseType.MultipleChoice,
            difficulty: 1,
            title: "Promoted Exercise",
            instructions: "Inst",
            contentJson: content,
            gradingDefinitionJson: "{}",
            maxPoints: 10m,
            gradingMode: GradingMode.Deterministic,
            contentHash: hash,
            now: now);

        exercise.Approve("admin", now);
        exercise.PromoteToOfficial("A1-03-E01", isCore: true, now);
        db.GrammarExercises.Add(exercise);
        await db.SaveChangesAsync();

        // User A tries to delete it -> should return 404 because exercise is now official and OwnerUserId is null
        var currentUser = new TestCurrentUser { UserId = "user-a" };
        var delHandler = new DeleteMyGrammarExerciseCommandHandler(db, uow, currentUser, TimeProvider.System);

        await Assert.ThrowsAsync<NotFoundException>(() => delHandler.HandleAsync(new DeleteMyGrammarExerciseCommand(exercise.Id)));
    }

    #endregion

    #region 4. Moderation & Promotion Tests

    [Fact]
    public async Task Moderation_ApproveReject_OnlyWhenPending_OtherwiseConflict409()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var step = await SeedStepAsync(db);

        var now = DateTimeOffset.UtcNow;
        string content = @"{""question"":""Q"",""selectionMode"":""Single"",""options"":[{""id"":""opt-a"",""text"":""a""}]}";
        string hash = GrammarContentHasher.ComputeHash(ExerciseType.MultipleChoice, content, "{}");

        var exercise = GrammarExerciseEntity.CreateUserOwned(
            stepId: step.Id,
            ownerUserId: "user-a",
            source: GrammarExerciseSource.UserAi,
            visibility: GrammarExerciseVisibility.Public,
            type: ExerciseType.MultipleChoice,
            difficulty: 1,
            title: "Pending Ex",
            instructions: "Inst",
            contentJson: content,
            gradingDefinitionJson: "{}",
            maxPoints: 10m,
            gradingMode: GradingMode.Deterministic,
            contentHash: hash,
            now: now);

        db.GrammarExercises.Add(exercise);
        await db.SaveChangesAsync();

        var adminUser = new TestCurrentUser { UserId = "admin-1", Roles = new List<string> { "ADMIN" } };
        var approveHandler = new ApproveGrammarExerciseCommandHandler(db, uow, adminUser, TimeProvider.System);
        var rejectHandler = new RejectGrammarExerciseCommandHandler(db, uow, adminUser, TimeProvider.System);

        // 1. Approve once -> Pending becomes Approved
        await approveHandler.HandleAsync(new ApproveGrammarExerciseCommand(exercise.Id));
        Assert.Equal(GrammarModerationStatus.Approved, exercise.Moderation);

        // 2. Approve again -> Conflict 409
        await Assert.ThrowsAsync<ConflictException>(() => approveHandler.HandleAsync(new ApproveGrammarExerciseCommand(exercise.Id)));

        // 3. Reject Approved exercise -> Conflict 409
        await Assert.ThrowsAsync<ConflictException>(() => rejectHandler.HandleAsync(new RejectGrammarExerciseCommand(exercise.Id, new RejectGrammarExerciseRequest("Reason"))));
    }

    [Fact]
    public async Task Moderation_PromoteToOfficial_PreservesAuthorUserId_SetsKeyAndIsCore()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var step = await SeedStepAsync(db);

        var now = DateTimeOffset.UtcNow;
        string content = @"{""question"":""Q"",""selectionMode"":""Single"",""options"":[{""id"":""opt-a"",""text"":""a""}]}";
        string hash = GrammarContentHasher.ComputeHash(ExerciseType.MultipleChoice, content, "{}");

        var exercise = GrammarExerciseEntity.CreateUserOwned(
            stepId: step.Id,
            ownerUserId: "author-user-123",
            source: GrammarExerciseSource.UserAi,
            visibility: GrammarExerciseVisibility.Public,
            type: ExerciseType.MultipleChoice,
            difficulty: 2,
            title: "Good Community Ex",
            instructions: "Inst",
            contentJson: content,
            gradingDefinitionJson: "{}",
            maxPoints: 10m,
            gradingMode: GradingMode.Deterministic,
            contentHash: hash,
            now: now);

        exercise.Approve("moderator", now);
        db.GrammarExercises.Add(exercise);
        await db.SaveChangesAsync();

        var promoteHandler = new PromoteGrammarExerciseCommandHandler(db, uow, TimeProvider.System);
        var adminDto = await promoteHandler.HandleAsync(new PromoteGrammarExerciseCommand(exercise.Id, new PromoteGrammarExerciseRequest(IsCore: true)));

        Assert.Equal(GrammarExerciseSource.Official, adminDto.Source);
        Assert.True(adminDto.IsCore);
        Assert.Equal("A1-03-E01", adminDto.Key);
        Assert.Null(adminDto.OwnerUserId);
        Assert.Equal("author-user-123", adminDto.AuthorUserId); // Author is preserved!
    }

    [Fact]
    public async Task AdminGenerateOfficial_CreatesOfficialExercisesWithGeneratedKeyAndIsCore()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var step = await SeedStepAsync(db);

        var fakeAi = new FakeGrammarExerciseAiGenerator();
        var contextLoader = new GrammarExercisePromptContextLoader(db);
        var promptBuilder = new GrammarExercisePromptBuilder();
        var processor = new GrammarExerciseOutputProcessor();

        var handler = new GenerateOfficialGrammarExercisesCommandHandler(
            db, uow, TimeProvider.System, contextLoader, promptBuilder, fakeAi, processor);

        var cmd = new GenerateOfficialGrammarExercisesCommand(
            StepCode: step.Code,
            Request: new GenerateOfficialGrammarExercisesRequest(
                Items: new List<GrammarExercisePromptRequestedItem> { new(ExerciseType.MultipleChoice, 1, 1) },
                IsCore: true
            )
        );

        var response = await handler.HandleAsync(cmd);

        Assert.Single(response.Created);
        Assert.Equal(GrammarExerciseSource.Official, response.Created[0].Source);
        Assert.True(response.Created[0].IsCore);
        Assert.Equal("A1-03-E01", response.Created[0].Key);
    }

    #endregion

    #region 5. Dedupe: Private of Other User is NOT Deduplicated

    [Fact]
    public async Task Dedupe_HashMatchingAnotherUsersPrivateExercise_IsNotDeduplicated()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);
        var step = await SeedStepAsync(db);

        var now = DateTimeOffset.UtcNow;
        string content = @"{""question"":""She ____ a nurse."",""selectionMode"":""Single"",""options"":[{""id"":""opt-a"",""text"":""is""},{""id"":""opt-b"",""text"":""am""}]}";
        string grading = @"{""correctOptionIds"":[""opt-a""],""explanation"":""She đi với is""}";
        string hash = GrammarContentHasher.ComputeHash(ExerciseType.MultipleChoice, content, grading);

        // User B has a PRIVATE exercise with this hash
        var userBExercise = GrammarExerciseEntity.CreateUserOwned(
            stepId: step.Id,
            ownerUserId: "user-b",
            source: GrammarExerciseSource.UserAi,
            visibility: GrammarExerciseVisibility.Private,
            type: ExerciseType.MultipleChoice,
            difficulty: 1,
            title: "User B Private",
            instructions: "Inst",
            contentJson: content,
            gradingDefinitionJson: grading,
            maxPoints: 10m,
            gradingMode: GradingMode.Deterministic,
            contentHash: hash,
            now: now);
        db.GrammarExercises.Add(userBExercise);
        await db.SaveChangesAsync();

        // User A generates an exercise with the SAME content
        var userACurrent = new TestCurrentUser { UserId = "user-a" };
        var fakeAi = new FakeGrammarExerciseAiGenerator();
        var contextLoader = new GrammarExercisePromptContextLoader(db);
        var promptBuilder = new GrammarExercisePromptBuilder();
        var processor = new GrammarExerciseOutputProcessor();

        var handler = new GenerateGrammarExercisesCommandHandler(
            db, uow, userACurrent, TimeProvider.System, contextLoader, promptBuilder, fakeAi, processor);

        var cmd = new GenerateGrammarExercisesCommand(
            StepCode: step.Code,
            Request: new GenerateGrammarExercisesRequest(
                Items: new List<GrammarExercisePromptRequestedItem> { new(ExerciseType.MultipleChoice, 1, 1) }
            )
        );

        var response = await handler.HandleAsync(cmd);

        // It should NOT be rejected as duplicate because user B's exercise is Private!
        Assert.Single(response.Created);
        Assert.Empty(response.Rejected);
    }

    #endregion

    #region G3 fix 1

    private static GrammarExerciseEntity NewUserExercise(Guid stepId, string owner, int i, GrammarExerciseVisibility vis, DateTimeOffset now) =>
        GrammarExerciseEntity.CreateUserOwned(
            stepId: stepId, ownerUserId: owner, source: GrammarExerciseSource.UserAi, visibility: vis,
            type: ExerciseType.MultipleChoice, difficulty: 1, title: $"Title {i}", instructions: "Inst",
            contentJson: "{}", gradingDefinitionJson: "{}", maxPoints: 10m, gradingMode: GradingMode.Deterministic,
            contentHash: i.ToString("x64"), now: now);

    [Fact]
    public async Task DailyQuota_CountsSoftDeletedExercises()
    {
        await using var db = new AppDbContext(CreateInMemoryOptions());
        var step = await SeedStepAsync(db);
        var now = DateTimeOffset.UtcNow;
        for (int i = 0; i < 40; i++)
        {
            var e = NewUserExercise(step.Id, "quota-user", i, GrammarExerciseVisibility.Private, now.AddHours(-1));
            e.SoftDelete(now, "quota-user");
            db.GrammarExercises.Add(e);
        }
        await db.SaveChangesAsync();

        var handler = new GenerateGrammarExercisesCommandHandler(
            db, new UnitOfWork(db), new TestCurrentUser { UserId = "quota-user" }, TimeProvider.System,
            new GrammarExercisePromptContextLoader(db), new GrammarExercisePromptBuilder(),
            new FakeGrammarExerciseAiGenerator(), new GrammarExerciseOutputProcessor());

        await Assert.ThrowsAsync<TooManyRequestsException>(() => handler.HandleAsync(new GenerateGrammarExercisesCommand(
            step.Code,
            new GenerateGrammarExercisesRequest(new List<GrammarExercisePromptRequestedItem> { new(ExerciseType.MultipleChoice, 1, 1) }))));
    }

    [Fact]
    public async Task AdminImport_CreatesOfficialExercises_WithoutCallingAi_AndSkipsSoftDeletedKeys()
    {
        await using var db = new AppDbContext(CreateInMemoryOptions());
        var step = await SeedStepAsync(db);
        var now = DateTimeOffset.UtcNow;
        var deleted = GrammarExerciseEntity.CreateOfficial(
            stepId: step.Id, key: "A1-03-E01", type: ExerciseType.MultipleChoice, difficulty: 1, title: "Old",
            instructions: "Inst", contentJson: "{}", gradingDefinitionJson: "{}", maxPoints: 10m,
            gradingMode: GradingMode.Deterministic, isCore: true, contentHash: new string('f', 64), now: now);
        deleted.SoftDelete(now, "admin");
        db.GrammarExercises.Add(deleted);
        await db.SaveChangesAsync();

        bool aiCalled = false;
        var ai = new FakeGrammarExerciseAiGenerator { OnGenerateInvoked = () => aiCalled = true };
        string json = await new FakeGrammarExerciseAiGenerator().GenerateRawAsync("", new GrammarAiExecutionConfig());
        var handler = new GenerateOfficialGrammarExercisesCommandHandler(
            db, new UnitOfWork(db), TimeProvider.System, new GrammarExercisePromptContextLoader(db),
            new GrammarExercisePromptBuilder(), ai, new GrammarExerciseOutputProcessor());

        var response = await handler.HandleAsync(new GenerateOfficialGrammarExercisesCommand(
            step.Code, new GenerateOfficialGrammarExercisesRequest([], IsCore: true), ImportJsonText: json));

        Assert.False(aiCalled);
        var created = Assert.Single(response.Created);
        Assert.Equal(GrammarExerciseSource.Official, created.Source);
        Assert.Equal("A1-03-E02", created.Key);
    }

    [Fact]
    public void PromptValidator_LearnerLimitedToTen_AdminToTwenty()
    {
        var validator = new EnglishLiveTutor.Application.Features.Grammar.Queries.BuildGrammarExercisePrompt.BuildGrammarExercisePromptValidator();
        var items = new List<GrammarExercisePromptRequestedItem> { new(ExerciseType.MultipleChoice, 11, 1) };
        var request = new BuildGrammarExercisePromptRequest(items);

        Assert.False(validator.Validate(new EnglishLiveTutor.Application.Features.Grammar.Queries.BuildGrammarExercisePrompt.BuildGrammarExercisePromptQuery("A1-03", request)).IsValid);
        Assert.True(validator.Validate(new EnglishLiveTutor.Application.Features.Grammar.Queries.BuildGrammarExercisePrompt.BuildGrammarExercisePromptQuery("A1-03", request, ForAdmin: true)).IsValid);
    }

    [Fact]
    public async Task ModerationQueue_NeverReturnsPrivateExercises()
    {
        await using var db = new AppDbContext(CreateInMemoryOptions());
        var step = await SeedStepAsync(db);
        db.GrammarExercises.Add(NewUserExercise(step.Id, "user-a", 1, GrammarExerciseVisibility.Private, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        var handler = new ListGrammarModerationQueueQueryHandler(db, new UserDirectory(db));
        var result = await handler.HandleAsync(new ListGrammarModerationQueueQuery(Status: GrammarModerationStatus.None));

        Assert.Empty(result.Items);
    }

    #endregion
}
