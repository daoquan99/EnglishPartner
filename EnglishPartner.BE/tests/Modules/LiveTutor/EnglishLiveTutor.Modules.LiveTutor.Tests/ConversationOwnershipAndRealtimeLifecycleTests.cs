using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Collections.Concurrent;
using EnglishLiveTutor.Api.Hubs;
using EnglishLiveTutor.Api.Realtime;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Conversations.Ports;
using EnglishLiveTutor.Application.Features.Conversations.Commands.CreateConversation;
using EnglishLiveTutor.Application.Features.Conversations.Commands.DeleteConversation;
using EnglishLiveTutor.Application.Features.Conversations.Commands.UpdateConversationSettings;
using EnglishLiveTutor.Application.Features.Conversations.Queries.GetConversationHistory;
using EnglishLiveTutor.Application.Features.Conversations.Queries.ListConversations;
using EnglishLiveTutor.Application.Features.Conversations.Validation;
using EnglishLiveTutor.Application.Modules;
using EnglishLiveTutor.Application.Modules.LiveTutor;
using EnglishLiveTutor.Domain.Conversations;
using EnglishLiveTutor.Domain.Conversations.Entities;
using EnglishLiveTutor.Domain.Conversations.Repositories;
using EnglishLiveTutor.Infrastructure.Ai;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Realtime;
using EnglishLiveTutor.Infrastructure.Repositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;
using LanguageCorrectionDto = EnglishLiveTutor.Application.Common.Models.LanguageCorrectionDto;

namespace EnglishLiveTutor.Domain.Tests;

public class ConversationOwnershipAndRealtimeLifecycleTests
{
    private sealed class TestDbFactory : IModuleDbContextFactory<ILiveTutorDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public TestDbFactory(string dbName)
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
        }

        public Task<ILiveTutorDbContext> CreateDbContextAsync(CancellationToken ct = default) =>
            Task.FromResult<ILiveTutorDbContext>(new AppDbContext(_options));
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public string? UserId { get; set; }

        public TestCurrentUser(string? userId)
        {
            UserId = userId;
        }
    }

    private sealed class RecordingTestNotifier : ILiveSessionRealtimeNotifier
    {
        public List<(string Target, string EventName, object? Payload)> Invocations { get; } = new();

        public Task SendTutorInterruptedAsync(string conversationId, CancellationToken ct = default)
        {
            Invocations.Add((conversationId, "TutorInterrupted", null));
            return Task.CompletedTask;
        }

        public Task SendTutorAudioAsync(string conversationId, string turnId, string base64Audio24k, string interactionStatus, CancellationToken ct = default)
        {
            Invocations.Add((conversationId, "ReceiveTutorAudio", new TutorAudioEventDto(conversationId, turnId, base64Audio24k, interactionStatus)));
            return Task.CompletedTask;
        }

        public Task SendInteractionStatusAsync(string conversationId, string status, string model, CancellationToken ct = default)
        {
            Invocations.Add((conversationId, "ReceiveInteractionStatus", new { conversationId, status, model }));
            return Task.CompletedTask;
        }

        public Task SendLanguageCorrectionAsync(string conversationId, LanguageCorrectionDto correction, CancellationToken ct = default)
        {
            Invocations.Add((conversationId, "ReceiveLanguageCorrection", correction));
            return Task.CompletedTask;
        }

        public Task SendTranscribeTranscriptAsync(string conversationId, string speaker, string turnId, int sequenceOrder, string text, bool isFinal, CancellationToken ct = default)
        {
            Invocations.Add((conversationId, "ReceiveTranscriptLine1", new TranscribeEventDto(conversationId, speaker, turnId, sequenceOrder, text, isFinal, DateTimeOffset.UtcNow)));
            return Task.CompletedTask;
        }

        public Task SendTranslateTextAsync(string conversationId, string speaker, string turnId, int sequenceOrder, string text, bool isFinal, CancellationToken ct = default)
        {
            Invocations.Add((conversationId, "ReceiveTranslationLine2", new TranslateEventDto(conversationId, speaker, turnId, sequenceOrder, text, isFinal, DateTimeOffset.UtcNow)));
            return Task.CompletedTask;
        }
    }

    private sealed class FailingRepository : IConversationRepository
    {
        public Task<List<ConversationSummary>> ListConversationsAsync(string userId, CancellationToken ct = default) =>
            throw new DbUpdateException("Database connection failed");
        public Task<(List<ConversationSummary> Items, int TotalCount)> ListConversationsPagedAsync(string userId, string? search = null, int? page = null, int? pageSize = null, CancellationToken ct = default) =>
            throw new DbUpdateException("Database connection failed");
        public Task<ConversationDetailRecord?> GetConversationDetailAsync(string userId, string id, CancellationToken ct = default) => throw new DbUpdateException("Database connection failed");
        public Task<ConversationEntity?> GetConversationWithHistoryAsync(string userId, string id, CancellationToken ct = default) =>
            throw new DbUpdateException("Database connection failed");
        public Task<IReadOnlyList<ChatMessageEntity>> GetRecentMessagesAsync(string userId, string conversationId, int take, CancellationToken ct = default) =>
            throw new DbUpdateException("Database connection failed");
        public Task<ConversationEntity?> GetConversationByIdAsync(string id, CancellationToken ct = default) =>
            throw new DbUpdateException("Database connection failed");
        public Task<ConversationEntity> CreateConversationAsync(string userId, string? id, string? title, string cefrLevel, bool extendedThinking, string voiceName, double speechSpeed, string conversationMode = "daily_conversation", string topic = "", string? customSystemPrompt = null, CancellationToken ct = default) =>
            throw new DbUpdateException("Database connection failed");
        public Task<ConversationEntity?> UpdateConversationSettingsAsync(string userId, string id, string? title, string cefrLevel, bool extendedThinking, string voiceName, double speechSpeed, string conversationMode = "daily_conversation", string topic = "", string? customSystemPrompt = null, CancellationToken ct = default) =>
            throw new DbUpdateException("Database connection failed");
        public Task<ConversationEntity> CreateOrUpdateConversationAsync(string userId, string? id, string? title, string cefrLevel, bool extendedThinking, string voiceName, double speechSpeed, string conversationMode = "daily_conversation", string topic = "", string? customSystemPrompt = null, CancellationToken ct = default) =>
            throw new DbUpdateException("Database connection failed");
        public Task<bool> DeleteConversationAsync(string userId, string id, CancellationToken ct = default) =>
            throw new DbUpdateException("Database connection failed");
        public Task<int> GetMaxSequenceOrderAsync(string conversationId, CancellationToken ct = default) =>
            Task.FromResult(0);
        public Task UpsertMessageTranscriptAsync(string conversationId, string turnId, string speaker, int sequenceOrder, string fullTranscript, bool isFinal, CancellationToken ct = default) =>
            throw new DbUpdateException("Database connection failed during transcript insert");
        public Task UpsertMessageTranslationAsync(string conversationId, string turnId, string speaker, int sequenceOrder, string fullVietnameseText, CancellationToken ct = default) =>
            throw new DbUpdateException("Database connection failed during translation insert");
        public Task<LanguageCorrectionEntity?> GetCorrectionByIdAsync(string conversationId, string correctionId, CancellationToken ct = default) =>
            throw new DbUpdateException("Database connection failed");
        public Task SaveCorrectionAsync(LanguageCorrectionEntity correction, CancellationToken ct = default) =>
            throw new DbUpdateException("Database connection failed during correction insert");
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage>? _responder;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage>? responder = null)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_responder != null)
            {
                return Task.FromResult(_responder(request));
            }

            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"responseData\":{\"translatedText\":\"dịch giả lập\"}}")
            };
            return Task.FromResult(response);
        }
    }

    private static (FastTextTranslatorFallback translator, GrammarCorrectionAnalyzerService grammar) CreateFakeAiServices(HttpMessageHandler? handler = null)
    {
        var h = handler ?? new FakeHttpMessageHandler();
        var translator = new FastTextTranslatorFallback(h);
        var grammar = new GrammarCorrectionAnalyzerService(h, translator);
        return (translator, grammar);
    }

    private sealed class DelayedRepository : IConversationRepository
    {
        private readonly IConversationRepository _inner;
        public TaskCompletionSource<bool> NonFinalTranscriptStartedTcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> NonFinalTranscriptReleaseTcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> NonFinalTranslationStartedTcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> NonFinalTranslationReleaseTcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public DelayedRepository(IConversationRepository inner) => _inner = inner;

        public Task<List<ConversationSummary>> ListConversationsAsync(string userId, CancellationToken ct = default) => _inner.ListConversationsAsync(userId, ct);
        public Task<(List<ConversationSummary> Items, int TotalCount)> ListConversationsPagedAsync(string userId, string? search = null, int? page = null, int? pageSize = null, CancellationToken ct = default) => _inner.ListConversationsPagedAsync(userId, search, page, pageSize, ct);
        public Task<ConversationDetailRecord?> GetConversationDetailAsync(string userId, string id, CancellationToken ct = default) => _inner.GetConversationDetailAsync(userId, id, ct);
        public Task<ConversationEntity?> GetConversationWithHistoryAsync(string userId, string id, CancellationToken ct = default) => _inner.GetConversationWithHistoryAsync(userId, id, ct);
        public Task<IReadOnlyList<ChatMessageEntity>> GetRecentMessagesAsync(string userId, string conversationId, int take, CancellationToken ct = default) => _inner.GetRecentMessagesAsync(userId, conversationId, take, ct);
        public Task<ConversationEntity?> GetConversationByIdAsync(string id, CancellationToken ct = default) => _inner.GetConversationByIdAsync(id, ct);
        public Task<ConversationEntity> CreateConversationAsync(string userId, string? id, string? title, string cefrLevel, bool extendedThinking, string voiceName, double speechSpeed, string conversationMode = "daily_conversation", string topic = "", string? customSystemPrompt = null, CancellationToken ct = default) => _inner.CreateConversationAsync(userId, id, title, cefrLevel, extendedThinking, voiceName, speechSpeed, conversationMode, topic, customSystemPrompt, ct);
        public Task<ConversationEntity?> UpdateConversationSettingsAsync(string userId, string id, string? title, string cefrLevel, bool extendedThinking, string voiceName, double speechSpeed, string conversationMode = "daily_conversation", string topic = "", string? customSystemPrompt = null, CancellationToken ct = default) => _inner.UpdateConversationSettingsAsync(userId, id, title, cefrLevel, extendedThinking, voiceName, speechSpeed, conversationMode, topic, customSystemPrompt, ct);
        public Task<ConversationEntity> CreateOrUpdateConversationAsync(string userId, string? id, string? title, string cefrLevel, bool extendedThinking, string voiceName, double speechSpeed, string conversationMode = "daily_conversation", string topic = "", string? customSystemPrompt = null, CancellationToken ct = default) => _inner.CreateOrUpdateConversationAsync(userId, id, title, cefrLevel, extendedThinking, voiceName, speechSpeed, conversationMode, topic, customSystemPrompt, ct);
        public Task<bool> DeleteConversationAsync(string userId, string id, CancellationToken ct = default) => _inner.DeleteConversationAsync(userId, id, ct);
        public Task<int> GetMaxSequenceOrderAsync(string conversationId, CancellationToken ct = default) => _inner.GetMaxSequenceOrderAsync(conversationId, ct);
        public Task<LanguageCorrectionEntity?> GetCorrectionByIdAsync(string conversationId, string correctionId, CancellationToken ct = default) => _inner.GetCorrectionByIdAsync(conversationId, correctionId, ct);
        public Task SaveCorrectionAsync(LanguageCorrectionEntity correction, CancellationToken ct = default) => _inner.SaveCorrectionAsync(correction, ct);

        public async Task UpsertMessageTranscriptAsync(string conversationId, string turnId, string speaker, int sequenceOrder, string fullTranscript, bool isFinal, CancellationToken ct = default)
        {
            if (!isFinal)
            {
                NonFinalTranscriptStartedTcs.TrySetResult(true);
                await NonFinalTranscriptReleaseTcs.Task;
            }

            await _inner.UpsertMessageTranscriptAsync(conversationId, turnId, speaker, sequenceOrder, fullTranscript, isFinal, ct);
        }

        public async Task UpsertMessageTranslationAsync(string conversationId, string turnId, string speaker, int sequenceOrder, string fullVietnameseText, CancellationToken ct = default)
        {
            if (fullVietnameseText.Contains("non-final", StringComparison.OrdinalIgnoreCase))
            {
                NonFinalTranslationStartedTcs.TrySetResult(true);
                await NonFinalTranslationReleaseTcs.Task;
            }

            await _inner.UpsertMessageTranslationAsync(conversationId, turnId, speaker, sequenceOrder, fullVietnameseText, ct);
        }
    }

    private sealed class ControllableBridge : GeminiLiveSessionBridge
    {
        public TaskCompletionSource<bool> ConnectStartedTcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> ConnectTcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool AudioReceived { get; private set; }
        public bool Disposed { get; private set; }

        public ControllableBridge(string conversationId, string roleTag) : base(conversationId, roleTag) { }

        public override async Task ConnectAndSetupAsync(string apiKey, object setupPayload, CancellationToken ct = default)
        {
            ConnectStartedTcs.TrySetResult(true);
            await ConnectTcs.Task;
        }

        public override bool IsOpen => !Disposed;

        public override Task SendRealtimeAudioAsync(string base64Pcm, CancellationToken ct = default)
        {
            AudioReceived = true;
            return Task.CompletedTask;
        }

        public override ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    // =========================================================================
    // 1. Domain Unit Tests
    // =========================================================================
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ConversationEntity_Create_ShouldThrowArgumentException_WhenUserIdIsNullOrWhiteSpace(string? invalidUserId)
    {
        Assert.Throws<ArgumentException>(() =>
            ConversationEntity.Create(invalidUserId!, "conv-1", "Title"));

        Assert.Throws<ArgumentException>(() =>
            new ConversationEntity(invalidUserId!, "conv-1", "Title"));
    }

    [Fact]
    public void ConversationEntity_Create_ShouldSetUserIdCorrectly()
    {
        var conv = ConversationEntity.Create("user-123", "conv-1", "Title A");
        Assert.Equal("user-123", conv.UserId);
        Assert.Equal("conv-1", conv.Id);
        Assert.Equal("Title A", conv.Title);
    }

    // =========================================================================
    // 2. REST CQRS Ownership Tests: User B cannot access User A's Conversations
    // =========================================================================
    [Fact]
    public async Task Rest_UserBCannotGet_Update_OrDelete_UserAConversation()
    {
        string dbName = Guid.NewGuid().ToString();
        var dbFactory = new TestDbFactory(dbName);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var repo = new ConversationRepository(dbFactory, timeProvider);

        var userA = new TestCurrentUser("user-a");
        var userB = new TestCurrentUser("user-b");

        // 1. User A creates a conversation
        var createHandlerA = new CreateConversationCommandHandler(repo, userA);
        var convA = await createHandlerA.HandleAsync(new CreateConversationCommand(
            new CreateConversationRequest("Hội thoại User A", "B2", false, "Aoede", 1.0, "daily_conversation", "Travel")));

        Assert.Equal("user-a", convA.UserId);
        string convId = convA.Id;

        // 2. User B tries to GET conversation of User A -> Returns null (404)
        var getHandlerB = new GetConversationHistoryQueryHandler(repo, userB);
        var resultGetB = await getHandlerB.HandleAsync(new GetConversationHistoryQuery(convId));
        Assert.Null(resultGetB);

        // User A gets their own conversation successfully
        var getHandlerA = new GetConversationHistoryQueryHandler(repo, userA);
        var resultGetA = await getHandlerA.HandleAsync(new GetConversationHistoryQuery(convId));
        Assert.NotNull(resultGetA);
        Assert.Equal("Hội thoại User A", resultGetA.Title);

        // 3. User B tries to LIST conversations -> Does NOT see User A's conversation
        var listHandlerB = new ListConversationsQueryHandler(repo, userB);
        var listB = await listHandlerB.HandleAsync(new ListConversationsQuery(null, 1, 10));
        Assert.Empty(listB.Items);

        // User A lists conversations -> Sees their conversation
        var listHandlerA = new ListConversationsQueryHandler(repo, userA);
        var listA = await listHandlerA.HandleAsync(new ListConversationsQuery(null, 1, 10));
        Assert.Single(listA.Items);
        Assert.Equal(convId, listA.Items[0].Id);

        // 4. User B tries to UPDATE settings of User A's conversation -> Returns null (404)
        var updateHandlerB = new UpdateConversationSettingsCommandHandler(repo, userB);
        var updateResultB = await updateHandlerB.HandleAsync(new UpdateConversationSettingsCommand(
            convId,
            new UpdateConversationSettingsRequest("Hacked Title", "C2", false, "Aoede", 1.0)));
        Assert.Null(updateResultB);

        // Check DB: Title remains unchanged
        var checkAfterUpdate = await getHandlerA.HandleAsync(new GetConversationHistoryQuery(convId));
        Assert.Equal("Hội thoại User A", checkAfterUpdate!.Title);

        // 5. User B tries to DELETE User A's conversation -> Returns false (404)
        var deleteHandlerB = new DeleteConversationCommandHandler(repo, userB);
        bool deletedByB = await deleteHandlerB.HandleAsync(new DeleteConversationCommand(convId));
        Assert.False(deletedByB);

        // Conversation still exists for User A
        var checkAfterDeleteAttempt = await getHandlerA.HandleAsync(new GetConversationHistoryQuery(convId));
        Assert.NotNull(checkAfterDeleteAttempt);

        // 6. User A deletes their own conversation -> Returns true
        var deleteHandlerA = new DeleteConversationCommandHandler(repo, userA);
        bool deletedByA = await deleteHandlerA.HandleAsync(new DeleteConversationCommand(convId));
        Assert.True(deletedByA);

        // Soft-deleted: no longer returned for User A
        var checkAfterDelete = await getHandlerA.HandleAsync(new GetConversationHistoryQuery(convId));
        Assert.Null(checkAfterDelete);
    }

    // =========================================================================
    // 3. Realtime Manager Ownership Tests: User B cannot Start, Forward, or Stop User A's session
    // =========================================================================
    [Fact]
    public async Task Manager_UserBCannotStart_Forward_OrStop_UserASession()
    {
        string dbName = Guid.NewGuid().ToString();
        var dbFactory = new TestDbFactory(dbName);
        var fakeClock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var repo = new ConversationRepository(dbFactory, fakeClock);

        // Pre-create conversation owned by User A in DB
        await repo.CreateConversationAsync(
            userId: "user-a",
            id: "conv-user-a",
            title: "Session A",
            cefrLevel: "B1",
            extendedThinking: false,
            voiceName: "Aoede",
            speechSpeed: 1.0);

        var (translator, grammar) = CreateFakeAiServices();

        var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            promptBuilder: new CefrPromptBuilder(),
            timeProvider: fakeClock,
            options: Options.Create(new LiveSessionOptions { IdleTimeoutMinutes = 15 }));

        // 1. User B cannot Start Tutor session on User A's conversation -> NotFoundException
        var startTutorReq = new StartTutorSessionRequest("conv-user-a", "test-api-key");
        var exTutor = await Assert.ThrowsAsync<NotFoundException>(() =>
            manager.StartTutorSessionAsync("user-b", "conn-b1", startTutorReq));
        Assert.Contains("Không tìm thấy hội thoại.", exTutor.Message);

        // 2. User B cannot Start Transcribe session on User A's conversation
        var startSubReq = new StartSubHubSessionRequest("conv-user-a", "test-api-key");
        var exTranscribe = await Assert.ThrowsAsync<NotFoundException>(() =>
            manager.StartTranscribeHubSessionAsync("user-b", "conn-b2", startSubReq));
        Assert.Contains("Không tìm thấy hội thoại.", exTranscribe.Message);

        // 3. User B cannot Start Translate session on User A's conversation
        var exTranslate = await Assert.ThrowsAsync<NotFoundException>(() =>
            manager.StartTranslateHubSessionAsync("user-b", "conn-b3", startSubReq));
        Assert.Contains("Không tìm thấy hội thoại.", exTranslate.Message);

        // Register active in-memory session for User A
        manager.RegisterActiveSessionForTesting("user-a", "conv-user-a", "api-key-a", "conn-a1", LiveHubType.Tutor);

        // 4. User B cannot forward audio to User A's session
        var exForwardTutor = await Assert.ThrowsAsync<NotFoundException>(() =>
            manager.ForwardTutorHubAudioAsync("user-b", "conn-b1", "conv-user-a", "dummy-pcm"));
        Assert.Contains("Không tìm thấy hội thoại.", exForwardTutor.Message);

        var exForwardTranscribe = await Assert.ThrowsAsync<NotFoundException>(() =>
            manager.ForwardTranscribeHubAudioAsync("user-b", "conn-b2", "conv-user-a", "dummy-pcm"));
        Assert.Contains("Không tìm thấy hội thoại.", exForwardTranscribe.Message);

        var exForwardTranslate = await Assert.ThrowsAsync<NotFoundException>(() =>
            manager.ForwardTranslateHubAudioAsync("user-b", "conn-b3", "conv-user-a", "dummy-pcm"));
        Assert.Contains("Không tìm thấy hội thoại.", exForwardTranslate.Message);

        // 5. User B cannot stop User A's session
        var exStop = await Assert.ThrowsAsync<NotFoundException>(() =>
            manager.StopAllForConversationAsync("user-b", "conn-b1", "conv-user-a"));
        Assert.Contains("Không tìm thấy hội thoại.", exStop.Message);

        manager.Dispose();
    }

    // =========================================================================
    // 4. Realtime Lifecycle Tests: Disconnect tutor stops session and clears API key
    // =========================================================================
    [Fact]
    public async Task Manager_DisconnectTutor_ClearsApiKey_State_AndSession()
    {
        string dbName = Guid.NewGuid().ToString();
        var dbFactory = new TestDbFactory(dbName);
        var fakeClock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var repo = new ConversationRepository(dbFactory, fakeClock);

        var (translator, grammar) = CreateFakeAiServices();

        var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            promptBuilder: new CefrPromptBuilder(),
            timeProvider: fakeClock,
            options: Options.Create(new LiveSessionOptions { IdleTimeoutMinutes = 15 }));

        manager.RegisterActiveSessionForTesting("user-a", "conv-1", "secret-byo-api-key", "conn-tutor-1", LiveHubType.Tutor);

        Assert.True(manager.HasActiveSession("conv-1"));
        Assert.True(manager.HasApiKey("conv-1"));
        Assert.True(manager.HasTurnState("conv-1"));

        // When Tutor connection disconnects
        await manager.OnClientDisconnectedAsync("conn-tutor-1");

        // Entire session state is cleaned up
        Assert.False(manager.HasActiveSession("conv-1"));
        Assert.False(manager.HasApiKey("conv-1"));
        Assert.False(manager.HasTurnState("conv-1"));

        manager.Dispose();
    }

    // =========================================================================
    // 5. Realtime Lifecycle Tests: Idle session cleaned up after timeout
    // =========================================================================
    [Fact]
    public async Task Manager_IdleSession_IsCleanedUp_AfterIdleTimeout()
    {
        string dbName = Guid.NewGuid().ToString();
        var dbFactory = new TestDbFactory(dbName);
        var fakeClock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 28, 12, 0, 0, TimeSpan.Zero));
        var repo = new ConversationRepository(dbFactory, fakeClock);

        var (translator, grammar) = CreateFakeAiServices();

        var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            promptBuilder: new CefrPromptBuilder(),
            timeProvider: fakeClock,
            options: Options.Create(new LiveSessionOptions { IdleTimeoutMinutes = 15 }));

        manager.RegisterActiveSessionForTesting("user-a", "conv-idle", "secret-key", "conn-1", LiveHubType.Tutor);
        Assert.True(manager.HasActiveSession("conv-idle"));

        // Advance by 10 minutes: Still active
        fakeClock.Advance(TimeSpan.FromMinutes(10));
        await manager.CleanupIdleSessionsAsync();
        Assert.True(manager.HasActiveSession("conv-idle"));

        // Advance by another 6 minutes (total 16 minutes > 15 minutes timeout)
        fakeClock.Advance(TimeSpan.FromMinutes(6));
        await manager.CleanupIdleSessionsAsync();

        // Idle session must be stopped and state removed
        Assert.False(manager.HasActiveSession("conv-idle"));
        Assert.False(manager.HasApiKey("conv-idle"));
        Assert.False(manager.HasTurnState("conv-idle"));

        manager.Dispose();
    }

    // =========================================================================
    // 6. Realtime Persistence Tests: Non-final is throttled, Final is immediately written
    // =========================================================================
    [Fact]
    public async Task Manager_PersistTranscript_ThrottlesNonFinal_AndImmediatelyWritesFinal()
    {
        string dbName = Guid.NewGuid().ToString();
        var dbFactory = new TestDbFactory(dbName);
        var t0 = new DateTimeOffset(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);
        var fakeClock = new FakeTimeProvider(t0);
        var repo = new ConversationRepository(dbFactory, fakeClock);

        // Pre-create conversation
        await repo.CreateConversationAsync("user-a", "conv-throttle", "Test Throttling", "B1", false, "Aoede", 1.0);

        var (translator, grammar) = CreateFakeAiServices();

        var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            promptBuilder: new CefrPromptBuilder(),
            timeProvider: fakeClock,
            options: Options.Create(new LiveSessionOptions { IdleTimeoutMinutes = 15 }));

        manager.RegisterActiveSessionForTesting("user-a", "conv-throttle", "key-1");
        var state = manager.GetTurnState("conv-throttle")!;

        // 1. First non-final transcript chunk -> should persist immediately because elapsed > 2s
        await manager.PersistTranscriptThrottledAsync("conv-throttle", state, "turn-1", "user", 1, "Hello", isFinal: false);

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var msg = await db.Messages.FirstOrDefaultAsync(m => m.TurnId == "turn-1");
            Assert.NotNull(msg);
            Assert.Equal("Hello", msg.TranscriptLine1);
            Assert.False(msg.IsFinal);
        }

        // 2. Second non-final chunk arrives 500ms later -> Throttled (< 2s)
        fakeClock.Advance(TimeSpan.FromMilliseconds(500));
        await manager.PersistTranscriptThrottledAsync("conv-throttle", state, "turn-1", "user", 1, "Hello world", isFinal: false);

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var msg = await db.Messages.FirstOrDefaultAsync(m => m.TurnId == "turn-1");
            Assert.NotNull(msg);
            // DB should still have "Hello", "Hello world" is pending!
            Assert.Equal("Hello", msg.TranscriptLine1);
        }

        // 3. Final transcript chunk arrives -> MUST persist immediately regardless of elapsed time
        await manager.PersistTranscriptThrottledAsync("conv-throttle", state, "turn-1", "user", 1, "Hello world, how are you?", isFinal: true);

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var msg = await db.Messages.FirstOrDefaultAsync(m => m.TurnId == "turn-1");
            Assert.NotNull(msg);
            Assert.Equal("Hello world, how are you?", msg.TranscriptLine1);
            Assert.True(msg.IsFinal);
        }

        // 4. Session stop flushes any pending non-final writes
        fakeClock.Advance(TimeSpan.FromSeconds(3));
        // Add a non-final for turn-2
        await manager.PersistTranscriptThrottledAsync("conv-throttle", state, "turn-2", "user", 2, "Turn 2 start", isFinal: false);
        // Another non-final immediately (< 2s)
        await manager.PersistTranscriptThrottledAsync("conv-throttle", state, "turn-2", "user", 2, "Turn 2 pending update", isFinal: false);

        // Before flush, DB has "Turn 2 start"
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var msg2 = await db.Messages.FirstOrDefaultAsync(m => m.TurnId == "turn-2");
            Assert.Equal("Turn 2 start", msg2!.TranscriptLine1);
        }

        // Flush on stop
        await manager.FlushPendingThrottledWritesAsync("conv-throttle");

        // After flush, DB has updated to pending "Turn 2 pending update"
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var msg2 = await db.Messages.FirstOrDefaultAsync(m => m.TurnId == "turn-2");
            Assert.Equal("Turn 2 pending update", msg2!.TranscriptLine1);
        }

        manager.Dispose();
    }

    // =========================================================================
    // 7. Race Final / Non-Final: Late non-final does not overwrite final transcript
    // =========================================================================
    [Fact]
    public void ChatMessageEntity_UpdateTranscript_NonFinalCannotOverwriteFinal()
    {
        var msg = ChatMessageEntity.CreateWithTranscript("conv-1", "turn-1", "user", 1, "Final text", isFinal: true);
        Assert.True(msg.IsFinal);
        Assert.Equal("Final text", msg.TranscriptLine1);

        // A delayed non-final arrives
        msg.UpdateTranscript("Older non-final partial text", isFinal: false, sequenceOrder: 1);

        // Must still retain final text!
        Assert.True(msg.IsFinal);
        Assert.Equal("Final text", msg.TranscriptLine1);
    }

    [Fact]
    public async Task Repository_UpsertTranscript_NonFinalCannotOverwriteCommittedFinalInDb()
    {
        string dbName = Guid.NewGuid().ToString();
        var dbFactory = new TestDbFactory(dbName);
        var repo = new ConversationRepository(dbFactory);

        await repo.CreateConversationAsync("user-a", "conv-race", "Race Test", "B1", false, "Aoede", 1.0);

        // 1. Final transcript is written
        await repo.UpsertMessageTranscriptAsync("conv-race", "turn-1", "user", 1, "I am going to Paris tomorrow.", isFinal: true);

        // 2. Slow non-final arrives later
        await repo.UpsertMessageTranscriptAsync("conv-race", "turn-1", "user", 1, "I am going to", isFinal: false);

        await using var db = await dbFactory.CreateDbContextAsync();
        var msg = await db.Messages.FirstOrDefaultAsync(m => m.ConversationId == "conv-race" && m.TurnId == "turn-1");
        Assert.NotNull(msg);
        Assert.Equal("I am going to Paris tomorrow.", msg.TranscriptLine1);
        Assert.True(msg.IsFinal);
    }

    // =========================================================================
    // 8. Error Isolation: Repository errors do not escape Manager to callers
    // =========================================================================
    [Fact]
    public async Task Manager_RepositoryErrors_AreIsolated_AndDoNotThrow()
    {
        var failingRepo = new FailingRepository();
        var (translator, grammar) = CreateFakeAiServices();

        var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: failingRepo,
            translator: translator,
            grammarAnalyzer: grammar,
            promptBuilder: new CefrPromptBuilder(),
            timeProvider: TimeProvider.System);

        manager.RegisterActiveSessionForTesting("user-a", "conv-fail", "test-key");
        var state = manager.GetTurnState("conv-fail")!;

        // PersistTranscriptThrottledAsync with DB error does not throw
        var ex1 = await Record.ExceptionAsync(() =>
            manager.PersistTranscriptThrottledAsync("conv-fail", state, "turn-1", "user", 1, "test transcript", isFinal: true));
        Assert.Null(ex1);

        // PersistTranslationThrottledAsync with DB error does not throw
        var ex2 = await Record.ExceptionAsync(() =>
            manager.PersistTranslationThrottledAsync("conv-fail", state, "turn-1", "user", 1, "bản dịch tiếng việt", isFinal: true));
        Assert.Null(ex2);

        // FinalizeTurnAsync with DB error does not throw
        var ex3 = await Record.ExceptionAsync(() =>
            manager.FinalizeTurnAsync("conv-fail", state, "user", "turn-1", 1));
        Assert.Null(ex3);

        // AnalyzeTurnGrammarManualAsync with DB error does not throw
        var ex4 = await Record.ExceptionAsync(() =>
            manager.AnalyzeTurnGrammarManualAsync("user-a", "test-conn-conv-fail-tutor", "conv-fail", "turn-1", "Hello test", "B1"));
        Assert.Null(ex4);

        manager.Dispose();
    }

    // =========================================================================
    // 9. Reconnect / Connection Mapping: Old connection disconnect does not stop new connection
    // =========================================================================
    [Fact]
    public async Task Manager_Reconnect_DisconnectOfOldConnection_DoesNotStopSessionOfNewConnection()
    {
        string dbName = Guid.NewGuid().ToString();
        var dbFactory = new TestDbFactory(dbName);
        var repo = new ConversationRepository(dbFactory);

        await repo.CreateConversationAsync("user-a", "conv-reconn", "Reconn Test", "B1", false, "Aoede", 1.0);

        var (translator, grammar) = CreateFakeAiServices();

        var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            promptBuilder: new CefrPromptBuilder(),
            timeProvider: TimeProvider.System);

        // 1. Connection 1 registers session
        manager.RegisterActiveSessionForTesting("user-a", "conv-reconn", "key-1", "conn-1", LiveHubType.Tutor);
        Assert.True(manager.HasActiveSession("conv-reconn"));

        // 2. Connection 2 reconnects on the same conversation
        manager.RegisterActiveSessionForTesting("user-a", "conv-reconn", "key-1", "conn-2", LiveHubType.Tutor);
        Assert.True(manager.HasActiveSession("conv-reconn"));

        // 3. Old connection 1 disconnects -> MUST NOT stop session of conn-2
        await manager.OnClientDisconnectedAsync("conn-1");
        Assert.True(manager.HasActiveSession("conv-reconn"));

        // 4. Current connection 2 disconnects -> MUST stop session
        await manager.OnClientDisconnectedAsync("conn-2");
        Assert.False(manager.HasActiveSession("conv-reconn"));

        manager.Dispose();
    }

    // =========================================================================
    // 10. Single Conversation per Connection: Starting different conv on same conn is rejected
    // =========================================================================
    [Fact]
    public async Task Manager_SameConnection_StartingDifferentConversation_IsRejected()
    {
        string dbName = Guid.NewGuid().ToString();
        var dbFactory = new TestDbFactory(dbName);
        var repo = new ConversationRepository(dbFactory);

        await repo.CreateConversationAsync("user-a", "conv-1", "First Conv", "B1", false, "Aoede", 1.0);
        await repo.CreateConversationAsync("user-a", "conv-2", "Second Conv", "B1", false, "Aoede", 1.0);

        var (translator, grammar) = CreateFakeAiServices();

        var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            promptBuilder: new CefrPromptBuilder(),
            timeProvider: TimeProvider.System);

        manager.RegisterActiveSessionForTesting("user-a", "conv-1", "key-1", "conn-x", LiveHubType.Tutor);

        // Starting conv-2 on conn-x must be rejected!
        var req = new StartTutorSessionRequest("conv-2", "key-1");
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            manager.StartTutorSessionAsync("user-a", "conn-x", req));
        Assert.Contains("Không tìm thấy hội thoại.", ex.Message);

        manager.Dispose();
    }

    // =========================================================================
    // 11. Cross-User Tests for EndUserSpeech, EndTranslateSpeech, AnalyzeTurnGrammar
    // =========================================================================
    [Fact]
    public async Task Manager_CrossUser_EndUserSpeech_EndTranslateSpeech_AnalyzeGrammar_ThrowsNotFound()
    {
        string dbName = Guid.NewGuid().ToString();
        var dbFactory = new TestDbFactory(dbName);
        var repo = new ConversationRepository(dbFactory);

        var (translator, grammar) = CreateFakeAiServices();

        var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            promptBuilder: new CefrPromptBuilder(),
            timeProvider: TimeProvider.System);

        manager.RegisterActiveSessionForTesting("user-a", "conv-owned-by-a", "key-a");

        // User B attempts to call speech ending or grammar analysis on User A's session -> NotFoundException
        await Assert.ThrowsAsync<NotFoundException>(() =>
            manager.EndUserSpeechAsync("user-b", "conn-b", "conv-owned-by-a"));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            manager.EndTranslateSpeechAsync("user-b", "conn-b", "conv-owned-by-a"));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            manager.AnalyzeTurnGrammarManualAsync("user-b", "conn-b", "conv-owned-by-a", "turn-1", "Hello", "B1"));

        manager.Dispose();
    }

    // =========================================================================
    // 12. Normalize and Validate ConversationId
    // =========================================================================
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("conv with space")]
    [InlineData("conv/with/slash")]
    [InlineData("conv<script>")]
    [InlineData("conv;drop table;")]
    public void ConversationIdValidator_InvalidValues_ThrowsNotFoundException(string? invalidId)
    {
        Assert.Throws<NotFoundException>(() =>
            ConversationIdValidator.NormalizeAndValidate(invalidId));
    }

    [Fact]
    public void ConversationIdValidator_TooLong_ThrowsNotFoundException()
    {
        string longId = new('a', 65);
        Assert.Throws<NotFoundException>(() =>
            ConversationIdValidator.NormalizeAndValidate(longId));
    }

    [Fact]
    public void ConversationIdValidator_Valid_TrimsAndAccepts()
    {
        string valid = "  conv_123-abc  ";
        string normalized = ConversationIdValidator.NormalizeAndValidate(valid);
        Assert.Equal("conv_123-abc", normalized);
    }

    // =========================================================================
    // 13. Group name format includes userId
    // =========================================================================
    [Fact]
    public async Task Manager_Notifier_BroadcastsToGroupNameWithUserId()
    {
        string dbName = Guid.NewGuid().ToString();
        var dbFactory = new TestDbFactory(dbName);
        var repo = new ConversationRepository(dbFactory);

        await repo.CreateConversationAsync("user-alice", "conv-group-test", "Group Test", "B1", false, "Aoede", 1.0);

        var recordingNotifier = new RecordingTestNotifier();
        var (translator, grammar) = CreateFakeAiServices();

        var manager = new GeminiLiveConnectionManager(
            notifier: recordingNotifier,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            promptBuilder: new CefrPromptBuilder(),
            timeProvider: TimeProvider.System);

        manager.RegisterActiveSessionForTesting("user-alice", "conv-group-test", "key-1");
        var state = manager.GetTurnState("conv-group-test")!;

        // Finalize a turn -> Triggers SendTranscribeTranscriptAsync
        state.TranscriptByTurn["turn-1"] = "Hello from Alice";
        await manager.FinalizeTurnAsync("conv-group-test", state, "user", "turn-1", 1);

        // Verify target group includes userId: "user-alice:conv-group-test"
        var transcriptCall = recordingNotifier.Invocations.FirstOrDefault(i => i.EventName == "ReceiveTranscriptLine1");
        Assert.Equal("user-alice:conv-group-test", transcriptCall.Target);

        manager.Dispose();
    }

    // =========================================================================
    // 14. Audio forwarding when no active session is silently ignored
    // =========================================================================
    [Fact]
    public async Task Manager_ForwardAudio_WhenNoSession_IsIgnoredSilently_WhenWrongUser_Throws()
    {
        string dbName = Guid.NewGuid().ToString();
        var dbFactory = new TestDbFactory(dbName);
        var repo = new ConversationRepository(dbFactory);

        var (translator, grammar) = CreateFakeAiServices();

        var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            promptBuilder: new CefrPromptBuilder(),
            timeProvider: TimeProvider.System);

        // 1. Conversation has NO active session -> ForwardAudio must NOT throw
        var ex1 = await Record.ExceptionAsync(() =>
            manager.ForwardTutorHubAudioAsync("user-a", "conn-a", "conv-inactive", "dummy-pcm"));
        Assert.Null(ex1);

        var ex2 = await Record.ExceptionAsync(() =>
            manager.ForwardTranscribeHubAudioAsync("user-a", "conn-a", "conv-inactive", "dummy-pcm"));
        Assert.Null(ex2);

        var ex3 = await Record.ExceptionAsync(() =>
            manager.ForwardTranslateHubAudioAsync("user-a", "conn-a", "conv-inactive", "dummy-pcm"));
        Assert.Null(ex3);

        // 2. Active session owned by user A -> user B calling forward throws NotFoundException
        manager.RegisterActiveSessionForTesting("user-a", "conv-inactive", "key-a", "conn-a1");
        await Assert.ThrowsAsync<NotFoundException>(() =>
            manager.ForwardTutorHubAudioAsync("user-b", "conn-b", "conv-inactive", "dummy-pcm"));

        manager.Dispose();
    }

    // =========================================================================
    // 15. DTO ToString overrides redact ApiKey
    // =========================================================================
    [Fact]
    public void StartSessionDtos_ToString_RedactsApiKey()
    {
        var tutorReq = new StartTutorSessionRequest("conv-1", "super-secret-gemini-key-999");
        string tutorStr = tutorReq.ToString();
        Assert.DoesNotContain("super-secret-gemini-key-999", tutorStr);
        Assert.Contains("[REDACTED]", tutorStr);

        var subReq = new StartSubHubSessionRequest("conv-1", "super-secret-subhub-key-888");
        string subStr = subReq.ToString();
        Assert.DoesNotContain("super-secret-subhub-key-888", subStr);
        Assert.Contains("[REDACTED]", subStr);
    }

    // =========================================================================
    // 16. HubExceptionHelper converts Application exceptions to HubException
    // =========================================================================
    [Fact]
    public async Task HubExceptionHelper_ConvertsNotFoundAndForbidden_ToStandardHubException()
    {
        // 1. NotFoundException -> HubException("Không tìm thấy hội thoại.")
        var ex1 = await Assert.ThrowsAsync<HubException>(() =>
            HubExceptionHelper.ExecuteAsync(() => throw new NotFoundException("User is not owner.")));
        Assert.Equal("Không tìm thấy hội thoại.", ex1.Message);

        // 2. ForbiddenException -> HubException("Không tìm thấy hội thoại.")
        var ex2 = await Assert.ThrowsAsync<HubException>(() =>
            HubExceptionHelper.ExecuteAsync(() => throw new ForbiddenException("No user id")));
        Assert.Equal("Không tìm thấy hội thoại.", ex2.Message);

        // 3. Unexpected exception -> Generic message without leaking internal details
        var ex3 = await Assert.ThrowsAsync<HubException>(() =>
            HubExceptionHelper.ExecuteAsync(() => throw new InvalidOperationException("Internal SQL query failed at line 42")));
        Assert.Equal("Đã xảy ra lỗi trong quá trình xử lý.", ex3.Message);
        Assert.DoesNotContain("Internal SQL", ex3.Message);
    }

    // =========================================================================
    // 17. F1 & F2: Final always wins for transcript and translation with controllable delay
    // =========================================================================
    [Fact]
    public async Task FinalAlwaysWins_TranscriptAndTranslation_WhenNonFinalRunsSlowAndFinishesAfterFinal()
    {
        string dbName = Guid.NewGuid().ToString();
        var dbFactory = new TestDbFactory(dbName);
        var baseRepo = new ConversationRepository(dbFactory);
        var delayedRepo = new DelayedRepository(baseRepo);

        await delayedRepo.CreateConversationAsync("user-1", "conv-race", "Race Test", "B1", false, "Aoede", 1.0);

        var (translator, grammar) = CreateFakeAiServices();
        var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: delayedRepo,
            translator: translator,
            grammarAnalyzer: grammar,
            promptBuilder: new CefrPromptBuilder(),
            timeProvider: TimeProvider.System);

        manager.RegisterActiveSessionForTesting("user-1", "conv-race", "key-1");
        var state = manager.GetTurnState("conv-race")!;

        // 1. Transcript race:
        // Start non-final transcript write in background (it will block inside repo)
        var nonFinalTranscriptTask = Task.Run(async () =>
        {
            await manager.PersistTranscriptThrottledAsync("conv-race", state, "turn-1", "user", 1, "non-final text", isFinal: false);
        });

        // Wait until non-final transcript has started and is waiting inside repo
        await delayedRepo.NonFinalTranscriptStartedTcs.Task;

        var finalTranscriptWaitingTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        manager.OnWaitingForTurnSemaphore = (tId, isFinal) =>
        {
            if (tId == "turn-1" && isFinal)
            {
                finalTranscriptWaitingTcs.TrySetResult(true);
            }
        };

        // Start final transcript write in background (waits on turn semaphore)
        var finalTranscriptTask = Task.Run(async () =>
        {
            await manager.PersistTranscriptThrottledAsync("conv-race", state, "turn-1", "user", 1, "FINAL TEXT", isFinal: true);
        });

        // Ensure final has queued and is waiting on turn semaphore before releasing non-final
        await finalTranscriptWaitingTcs.Task;

        // Now release the non-final write
        delayedRepo.NonFinalTranscriptReleaseTcs.SetResult(true);
        await nonFinalTranscriptTask;
        await finalTranscriptTask;

        // Non-final arriving after final is ignored
        await manager.PersistTranscriptThrottledAsync("conv-race", state, "turn-1", "user", 1, "late non-final", isFinal: false);

        // Verify in DB that final transcript is preserved
        var conv = await baseRepo.GetConversationWithHistoryAsync("user-1", "conv-race");
        var msg = conv!.Messages.FirstOrDefault(m => m.TurnId == "turn-1");
        Assert.NotNull(msg);
        Assert.Equal("FINAL TEXT", msg.TranscriptLine1);
        Assert.True(msg.IsFinal);

        // Direct repository race: non-final completing AFTER final cannot downgrade
        var delayedRepo2 = new DelayedRepository(baseRepo);
        var directNonFinalTask = Task.Run(async () =>
        {
            await delayedRepo2.UpsertMessageTranscriptAsync("conv-race", "turn-1", "user", 1, "downgraded text", isFinal: false);
        });
        await delayedRepo2.NonFinalTranscriptStartedTcs.Task;
        await baseRepo.UpsertMessageTranscriptAsync("conv-race", "turn-1", "user", 1, "FINAL TEXT", isFinal: true);
        delayedRepo2.NonFinalTranscriptReleaseTcs.SetResult(true);
        await directNonFinalTask;

        conv = await baseRepo.GetConversationWithHistoryAsync("user-1", "conv-race");
        msg = conv!.Messages.FirstOrDefault(m => m.TurnId == "turn-1");
        Assert.NotNull(msg);
        Assert.Equal("FINAL TEXT", msg.TranscriptLine1);
        Assert.True(msg.IsFinal);

        // 2. Translation race:
        var nonFinalTranslationTask = Task.Run(async () =>
        {
            await manager.PersistTranslationThrottledAsync("conv-race", state, "turn-2", "user", 2, "bản dịch non-final", isFinal: false);
        });

        await delayedRepo.NonFinalTranslationStartedTcs.Task;

        var finalTranslationWaitingTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        manager.OnWaitingForTurnSemaphore = (tId, isFinal) =>
        {
            if (tId == "turn-2" && isFinal)
            {
                finalTranslationWaitingTcs.TrySetResult(true);
            }
        };

        // Start final translation write in background (waits on turn semaphore)
        var finalTranslationTask = Task.Run(async () =>
        {
            await manager.PersistTranslationThrottledAsync("conv-race", state, "turn-2", "user", 2, "BẢN DỊCH FINAL", isFinal: true);
        });

        // Ensure final translation has queued and is waiting on turn semaphore
        await finalTranslationWaitingTcs.Task;

        // Release delayed non-final
        delayedRepo.NonFinalTranslationReleaseTcs.SetResult(true);
        await nonFinalTranslationTask;
        await finalTranslationTask;

        // Late non-final after final is ignored
        await manager.PersistTranslationThrottledAsync("conv-race", state, "turn-2", "user", 2, "bản dịch late non-final", isFinal: false);

        // Verify in DB that final translation is preserved
        conv = await baseRepo.GetConversationWithHistoryAsync("user-1", "conv-race");
        var msg2 = conv!.Messages.FirstOrDefault(m => m.TurnId == "turn-2");
        Assert.NotNull(msg2);
        Assert.Equal("BẢN DỊCH FINAL", msg2.TranslationLine2);

        manager.Dispose();
    }

    // =========================================================================
    // 18. F3: Start tutor connecting + transcribe disconnect does not clean up session
    // =========================================================================
    [Fact]
    public async Task F3_TutorStartConnecting_PlusTranscribeDisconnect_DoesNotCleanUpSession()
    {
        string dbName = Guid.NewGuid().ToString();
        var dbFactory = new TestDbFactory(dbName);
        var repo = new ConversationRepository(dbFactory);

        await repo.CreateConversationAsync("user-1", "conv-f3", "F3 Test", "B1", false, "Aoede", 1.0);

        var (translator, grammar) = CreateFakeAiServices();
        var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            promptBuilder: new CefrPromptBuilder(),
            timeProvider: TimeProvider.System);

        // Register old transcribe connection
        manager.RegisterActiveSessionForTesting("user-1", "conv-f3", "key-1", "conn-transcribe-old", LiveHubType.Transcribe);

        var controllableBridge = new ControllableBridge("conv-f3", "tutor");
        manager.BridgeFactory = (convId, role) => controllableBridge;

        // Start Tutor session in background -> It blocks on ConnectAndSetupAsync
        var startTutorTask = Task.Run(async () =>
        {
            await manager.StartTutorSessionAsync("user-1", "conn-tutor-new", new StartTutorSessionRequest("conv-f3", "key-1"));
        });

        // Wait until Tutor session has started connecting
        await controllableBridge.ConnectStartedTcs.Task;

        // Transcribe disconnects while Tutor is still starting
        await manager.OnClientDisconnectedAsync("conn-transcribe-old");

        // Verify state is NOT cleaned up
        Assert.True(manager.HasActiveSession("conv-f3"));
        Assert.True(manager.HasApiKey("conv-f3"));

        // Complete Tutor connection
        controllableBridge.ConnectTcs.SetResult(true);
        await startTutorTask;

        // Audio should be accepted
        await manager.ForwardTutorHubAudioAsync("user-1", "conn-tutor-new", "conv-f3", "base64audio");
        Assert.True(controllableBridge.AudioReceived);

        manager.Dispose();
    }

    // =========================================================================
    // 19. F5: Replaced old connection audio ignored silently, commands throw NotFound
    // =========================================================================
    [Fact]
    public async Task F5_ReplacedOldConnection_AudioSilentlyIgnored_OtherCommandsThrowNotFound()
    {
        string dbName = Guid.NewGuid().ToString();
        var dbFactory = new TestDbFactory(dbName);
        var repo = new ConversationRepository(dbFactory);

        await repo.CreateConversationAsync("user-1", "conv-f5", "F5 Test", "B1", false, "Aoede", 1.0);

        var (translator, grammar) = CreateFakeAiServices();
        var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            promptBuilder: new CefrPromptBuilder(),
            timeProvider: TimeProvider.System);

        // Connection 1 registered
        manager.RegisterActiveSessionForTesting("user-1", "conv-f5", "key-1", "conn-old", LiveHubType.Tutor);

        // Replaced by Connection 2
        manager.RegisterActiveSessionForTesting("user-1", "conv-f5", "key-1", "conn-new", LiveHubType.Tutor);

        // 1. Audio from old connection is silently ignored (does not throw)
        var exAudio = await Record.ExceptionAsync(() =>
            manager.ForwardTutorHubAudioAsync("user-1", "conn-old", "conv-f5", "base64audio"));
        Assert.Null(exAudio);

        // 2. StopAll from old connection throws NotFoundException
        var exStop = await Assert.ThrowsAsync<NotFoundException>(() =>
            manager.StopAllForConversationAsync("user-1", "conn-old", "conv-f5"));
        Assert.Contains("Không tìm thấy hội thoại.", exStop.Message);

        // 3. AnalyzeTurnGrammarManual from old connection throws NotFoundException
        var exAnalyze = await Assert.ThrowsAsync<NotFoundException>(() =>
            manager.AnalyzeTurnGrammarManualAsync("user-1", "conn-old", "conv-f5", "turn-1", "test text", "B1"));
        Assert.Contains("Không tìm thấy hội thoại.", exAnalyze.Message);

        // 4. StopAll from new connection succeeds
        var exNewStop = await Record.ExceptionAsync(() =>
            manager.StopAllForConversationAsync("user-1", "conn-new", "conv-f5"));
        Assert.Null(exNewStop);

        manager.Dispose();
    }

    // =========================================================================
    // 20. F6: Turn resources are cleaned up after finalize
    // =========================================================================
    [Fact]
    public async Task F6_TurnResources_AreCleanedUp_AfterFinalizeTurn()
    {
        string dbName = Guid.NewGuid().ToString();
        var dbFactory = new TestDbFactory(dbName);
        var repo = new ConversationRepository(dbFactory);

        await repo.CreateConversationAsync("user-1", "conv-f6", "F6 Test", "B1", false, "Aoede", 1.0);

        var grammarCalledTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var fakeHttp = new FakeHttpMessageHandler(req =>
        {
            if (req.RequestUri?.ToString().Contains("generativelanguage.googleapis.com") == true)
            {
                grammarCalledTcs.TrySetResult(true);
            }
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            };
        });

        var (translator, grammar) = CreateFakeAiServices(fakeHttp);
        var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            promptBuilder: new CefrPromptBuilder(),
            timeProvider: TimeProvider.System);

        var grammarCompletedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        manager.OnGrammarCheckCompleted = () => grammarCompletedTcs.TrySetResult(true);

        manager.RegisterActiveSessionForTesting("user-1", "conv-f6", "key-1");
        var state = manager.GetTurnState("conv-f6")!;

        string turnId = "turn-cleanup-test";
        state.TranscriptByTurn[turnId] = "Hello world";
        state.TranslationByTurn[turnId] = "Xin chào";
        state.PendingTranscriptByTurn[turnId] = ("user", 1, "Hello world");
        state.PendingTranslationByTurn[turnId] = ("user", 1, "Xin chào");

        // Finalize turn
        await manager.FinalizeTurnAsync("conv-f6", state, "user", turnId, 1);

        // Verify grammar final still runs and fake analyzer recorded the call
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await grammarCalledTcs.Task.WaitAsync(timeoutCts.Token);
        Assert.True(grammarCalledTcs.Task.IsCompletedSuccessfully);

        // Wait until background grammar check finishes and cleans up
        await grammarCompletedTcs.Task.WaitAsync(timeoutCts.Token);

        // Verify turn dictionary entries are cleaned up
        Assert.False(state.TranscriptByTurn.ContainsKey(turnId));
        Assert.False(state.TranslationByTurn.ContainsKey(turnId));
        Assert.False(state.PendingTranscriptByTurn.ContainsKey(turnId));
        Assert.False(state.PendingTranslationByTurn.ContainsKey(turnId));
        Assert.False(state.TranscriptThrottleCtsByTurn.ContainsKey(turnId));
        Assert.False(state.TranslationThrottleCtsByTurn.ContainsKey(turnId));
        Assert.False(state.TranslationDebounceByTurn.ContainsKey(turnId));
        Assert.False(state.GrammarDebounceByTurn.ContainsKey(turnId));
        Assert.False(state.GrammarFinalCtsByTurn.ContainsKey(turnId));
        Assert.False(state.TranscriptSemaphoresByTurn.ContainsKey(turnId));
        Assert.False(state.TranslationSemaphoresByTurn.ContainsKey(turnId));

        manager.Dispose();
    }

    // =========================================================================
    // 21. F4 Fix: Parallel transcribe Start failure does not rollback active Tutor session
    // =========================================================================
    [Fact]
    public async Task StartTranscribeFailure_ParallelToActiveTutor_DoesNotRollbackTutorSession()
    {
        string dbName = Guid.NewGuid().ToString();
        var dbFactory = new TestDbFactory(dbName);
        var repo = new ConversationRepository(dbFactory);

        await repo.CreateConversationAsync("user-1", "conv-f4-fix", "F4 Fix Test", "B1", false, "Aoede", 1.0);

        var (translator, grammar) = CreateFakeAiServices();
        var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            promptBuilder: new CefrPromptBuilder(),
            timeProvider: TimeProvider.System);

        var tutorBridge = new ControllableBridge("conv-f4-fix", "tutor");
        tutorBridge.ConnectTcs.SetResult(true);

        var transcribeBridge = new ControllableBridge("conv-f4-fix", "transcribe");

        manager.BridgeFactory = (convId, role) =>
        {
            if (role == "tutor") return tutorBridge;
            if (role == "transcribe")
            {
                // Simulate connect failure for Transcribe
                transcribeBridge.ConnectTcs.SetException(new InvalidOperationException("Transcribe connect error"));
                return transcribeBridge;
            }
            throw new ArgumentException("Unknown role");
        };

        // 1. Tutor starts successfully
        await manager.StartTutorSessionAsync("user-1", "conn-tutor", new StartTutorSessionRequest("conv-f4-fix", "valid-key"));

        // 2. Transcribe starts in parallel and fails
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            manager.StartTranscribeHubSessionAsync("user-1", "conn-transcribe", new StartSubHubSessionRequest("conv-f4-fix", "valid-key")));

        // 3. Verify session owner and API key are still present
        Assert.True(manager.HasActiveSession("conv-f4-fix"));
        Assert.True(manager.HasApiKey("conv-f4-fix"));
        Assert.True(manager.HasTurnState("conv-f4-fix"));

        // 4. Verify Tutor audio is still accepted
        await manager.ForwardTutorHubAudioAsync("user-1", "conn-tutor", "conv-f4-fix", "base64audio");
        Assert.True(tutorBridge.AudioReceived);

        // 5. Verify StopAllForConversation of Tutor still succeeds
        var exStop = await Record.ExceptionAsync(() =>
            manager.StopAllForConversationAsync("user-1", "conn-tutor", "conv-f4-fix"));
        Assert.Null(exStop);
        Assert.False(manager.HasActiveSession("conv-f4-fix"));

        manager.Dispose();
    }

    // =========================================================================
    // 22. Orphan Bridge Prevention: Connection disconnect during connect aborts bridge and cleans up state
    // =========================================================================
    [Fact]
    public async Task ConnectionDisconnect_DuringConnectAndSetup_DisposesBridgeAndCleansUpSession()
    {
        string dbName = Guid.NewGuid().ToString();
        var dbFactory = new TestDbFactory(dbName);
        var repo = new ConversationRepository(dbFactory);

        await repo.CreateConversationAsync("user-1", "conv-orphan", "Orphan Test", "B1", false, "Aoede", 1.0);

        var (translator, grammar) = CreateFakeAiServices();
        var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            promptBuilder: new CefrPromptBuilder(),
            timeProvider: TimeProvider.System);

        var controllableBridge = new ControllableBridge("conv-orphan", "tutor");
        manager.BridgeFactory = (convId, role) => controllableBridge;

        // 1. Start Tutor session in background -> blocked waiting for ConnectTcs
        var startTask = Task.Run(async () =>
        {
            await manager.StartTutorSessionAsync("user-1", "conn-orphan", new StartTutorSessionRequest("conv-orphan", "key-1"));
        });

        // Wait until connection begins
        await controllableBridge.ConnectStartedTcs.Task;

        // 2. Client disconnects while bridge connect is blocked
        await manager.OnClientDisconnectedAsync("conn-orphan");

        // 3. Now let ConnectAndSetupAsync finish
        controllableBridge.ConnectTcs.SetResult(true);
        await startTask;

        // 4. Verify bridge is disposed, not in _bridges, and session state is cleaned up
        Assert.True(controllableBridge.Disposed);
        Assert.False(manager.HasBridge("conv-orphan:tutor"));
        Assert.False(manager.HasActiveSession("conv-orphan"));
        Assert.False(manager.HasApiKey("conv-orphan"));
        Assert.False(manager.HasTurnState("conv-orphan"));

        manager.Dispose();
    }

    // =========================================================================
    // 23. Rollback of a superseded Start must not remove the bridge of the newer Start
    // =========================================================================
    [Fact]
    public async Task SupersededStart_RollbackAfterNewerStartSucceeded_KeepsNewerBridge()
    {
        string dbName = Guid.NewGuid().ToString();
        var dbFactory = new TestDbFactory(dbName);
        var repo = new ConversationRepository(dbFactory);

        await repo.CreateConversationAsync("user-1", "conv-supersede", "Supersede Test", "B1", false, "Aoede", 1.0);

        var (translator, grammar) = CreateFakeAiServices();
        var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            promptBuilder: new CefrPromptBuilder(),
            timeProvider: TimeProvider.System);

        var oldBridge = new ControllableBridge("conv-supersede", "tutor");
        var newBridge = new ControllableBridge("conv-supersede", "tutor");
        newBridge.ConnectTcs.SetResult(true);
        int created = 0;
        manager.BridgeFactory = (convId, role) => Interlocked.Increment(ref created) == 1 ? oldBridge : newBridge;

        // 1. Start on the old connection blocks while connecting
        var oldStart = Task.Run(() => manager.StartTutorSessionAsync("user-1", "conn-old", new StartTutorSessionRequest("conv-supersede", "key-old")));
        await oldBridge.ConnectStartedTcs.Task;

        // 2. Reconnect: Start on a new connection completes and publishes its bridge
        await manager.StartTutorSessionAsync("user-1", "conn-new", new StartTutorSessionRequest("conv-supersede", "key-new"));
        Assert.True(manager.HasBridge("conv-supersede:tutor"));

        // 3. The superseded Start finishes connecting, notices it is no longer current and rolls back
        oldBridge.ConnectTcs.SetResult(true);
        await oldStart;

        // 4. Only the old bridge is gone; the newer session keeps its bridge, key and audio path
        Assert.True(oldBridge.Disposed);
        Assert.False(newBridge.Disposed);
        Assert.True(manager.HasBridge("conv-supersede:tutor"));
        Assert.True(manager.HasActiveSession("conv-supersede"));
        Assert.True(manager.HasApiKey("conv-supersede"));

        await manager.ForwardTutorHubAudioAsync("user-1", "conn-new", "conv-supersede", "dummy-pcm");
        Assert.True(newBridge.AudioReceived);

        manager.Dispose();
    }

    // =========================================================================
    // 24. Live session start reads only the transcript tail, scoped to the owner
    // =========================================================================
    [Fact]
    public async Task Repository_GetRecentMessages_ReturnsLastTurnsOldestFirst_OnlyForOwner()
    {
        string dbName = Guid.NewGuid().ToString();
        var repo = new ConversationRepository(new TestDbFactory(dbName));
        await repo.CreateConversationAsync("user-1", "conv-tail", "Tail Test", "B1", false, "Aoede", 1.0);

        for (int i = 1; i <= 20; i++)
        {
            await repo.UpsertMessageTranscriptAsync("conv-tail", $"t{i}", i % 2 == 0 ? "ai" : "user", i, $"line {i}", isFinal: true);
        }

        var recent = await repo.GetRecentMessagesAsync("user-1", "conv-tail", 16);

        Assert.Equal(16, recent.Count);
        Assert.Equal(Enumerable.Range(5, 16).ToArray(), recent.Select(m => m.SequenceOrder).ToArray());
        Assert.Empty(await repo.GetRecentMessagesAsync("user-2", "conv-tail", 16));
    }

    // =========================================================================
    // 25. Grammar Hub Realtime Lifecycle, Ownership and Optimization Tests (Prompt AGY R2)
    // =========================================================================

    private sealed class SignalRTestClientProxy : ISingleClientProxy
    {
        public List<(string Method, object?[] Args)> Invocations { get; } = new();

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            Invocations.Add((method, args));
            return Task.CompletedTask;
        }

        public Task<T> InvokeCoreAsync<T>(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            Invocations.Add((method, args));
            return Task.FromResult(default(T)!);
        }
    }

    private sealed class SignalRTestHubClients : IHubClients
    {
        public ConcurrentDictionary<string, SignalRTestClientProxy> GroupProxies { get; } = new();

        public IClientProxy Group(string groupName) =>
            GroupProxies.GetOrAdd(groupName, _ => new SignalRTestClientProxy());

        public IClientProxy All => throw new NotImplementedException();
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotImplementedException();
        public IClientProxy Client(string connectionId) => throw new NotImplementedException();
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotImplementedException();
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotImplementedException();
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => throw new NotImplementedException();
        public IClientProxy User(string userId) => throw new NotImplementedException();
        public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotImplementedException();
    }

    private sealed class SignalRTestHubContext<THub> : IHubContext<THub> where THub : Hub
    {
        public SignalRTestHubClients ClientsProxy { get; } = new();
        public IHubClients Clients => ClientsProxy;
        public IGroupManager Groups => throw new NotImplementedException();
    }

    [Fact]
    public async Task StartGrammarHubSession_UserB_OnUserAConversationInDb_ThrowsNotFoundException_AndDoesNotRegisterConnection()
    {
        string dbName = Guid.NewGuid().ToString();
        var repo = new ConversationRepository(new TestDbFactory(dbName));
        await repo.CreateConversationAsync("user-a", "conv-in-db", "DB Conv", "B1", false, "Aoede", 1.0);

        var (translator, grammar) = CreateFakeAiServices();
        using var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            promptBuilder: new CefrPromptBuilder(),
            timeProvider: TimeProvider.System);

        var request = new StartSubHubSessionRequest("conv-in-db", "key-b");
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            manager.StartGrammarHubSessionAsync("user-b", "conn-b-grammar", request));

        Assert.Contains("Không tìm thấy hội thoại.", ex.Message);
        Assert.False(manager.HasConnection("conn-b-grammar"));
        Assert.False(manager.HasHubConnection("conv-in-db", LiveHubType.Grammar));
    }

    [Fact]
    public async Task StartGrammarHubSession_UserB_OnUserAConversationInMemoryOnly_ThrowsNotFoundException_AndDoesNotRegisterConnection()
    {
        string dbName = Guid.NewGuid().ToString();
        var repo = new ConversationRepository(new TestDbFactory(dbName));
        var (translator, grammar) = CreateFakeAiServices();
        using var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            promptBuilder: new CefrPromptBuilder(),
            timeProvider: TimeProvider.System);

        manager.RegisterActiveSessionForTesting("user-a", "conv-in-mem", "api-key-a", "conn-a-tutor", LiveHubType.Tutor);

        var request = new StartSubHubSessionRequest("conv-in-mem", "key-b");
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            manager.StartGrammarHubSessionAsync("user-b", "conn-b-grammar", request));

        Assert.Contains("Không tìm thấy hội thoại.", ex.Message);
        Assert.False(manager.HasConnection("conn-b-grammar"));
        Assert.False(manager.HasHubConnection("conv-in-mem", LiveHubType.Grammar));
    }

    [Fact]
    public async Task StartGrammarHubSession_OnSoftDeletedConversation_ThrowsNotFoundException()
    {
        string dbName = Guid.NewGuid().ToString();
        var repo = new ConversationRepository(new TestDbFactory(dbName));
        await repo.CreateConversationAsync("user-a", "conv-soft-del", "Soft Deleted", "B1", false, "Aoede", 1.0);
        await repo.DeleteConversationAsync("user-a", "conv-soft-del");

        var (translator, grammar) = CreateFakeAiServices();
        using var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            promptBuilder: new CefrPromptBuilder(),
            timeProvider: TimeProvider.System);

        var request = new StartSubHubSessionRequest("conv-soft-del", "key-a");
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            manager.StartGrammarHubSessionAsync("user-a", "conn-a-grammar", request));

        Assert.Contains("Không tìm thấy hội thoại.", ex.Message);
        Assert.False(manager.HasConnection("conn-a-grammar"));
        Assert.False(manager.HasHubConnection("conv-soft-del", LiveHubType.Grammar));
    }

    [Fact]
    public async Task AnalyzeTurnGrammarManual_UserB_OnUserAConnectionOrSession_ThrowsNotFoundException()
    {
        string dbName = Guid.NewGuid().ToString();
        var repo = new ConversationRepository(new TestDbFactory(dbName));
        var (translator, grammar) = CreateFakeAiServices();
        using var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            promptBuilder: new CefrPromptBuilder(),
            timeProvider: TimeProvider.System);

        manager.RegisterActiveSessionForTesting("user-a", "conv-a", "key-a", "conn-a-grammar", LiveHubType.Grammar);

        // 1. User B tries to call using User A's connection -> NotFoundException
        var ex1 = await Assert.ThrowsAsync<NotFoundException>(() =>
            manager.AnalyzeTurnGrammarManualAsync("user-b", "conn-a-grammar", "conv-a", "turn-1", "I goes", "B1"));
        Assert.Contains("Không tìm thấy hội thoại.", ex1.Message);

        // 2. User B tries to call using their own unregistered connection -> NotFoundException
        var ex2 = await Assert.ThrowsAsync<NotFoundException>(() =>
            manager.AnalyzeTurnGrammarManualAsync("user-b", "conn-b-fake", "conv-a", "turn-1", "I goes", "B1"));
        Assert.Contains("Không tìm thấy hội thoại.", ex2.Message);
    }

    [Fact]
    public async Task GrammarConnectionDisconnect_WhileTutorActive_KeepsTutorSessionActive()
    {
        string dbName = Guid.NewGuid().ToString();
        var repo = new ConversationRepository(new TestDbFactory(dbName));
        var (translator, grammar) = CreateFakeAiServices();
        using var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            promptBuilder: new CefrPromptBuilder(),
            timeProvider: TimeProvider.System);

        manager.RegisterActiveSessionForTesting("user-a", "conv-live", "key-a", "conn-tutor", LiveHubType.Tutor);
        manager.RegisterActiveSessionForTesting("user-a", "conv-live", "key-a", "conn-grammar", LiveHubType.Grammar);

        Assert.True(manager.HasActiveSession("conv-live"));
        Assert.True(manager.HasApiKey("conv-live"));
        Assert.True(manager.HasConnection("conn-tutor"));
        Assert.True(manager.HasConnection("conn-grammar"));

        // Disconnect grammar connection
        await manager.OnClientDisconnectedAsync("conn-grammar");

        // Tutor session remains active and unchanged
        Assert.True(manager.HasActiveSession("conv-live"));
        Assert.True(manager.HasApiKey("conv-live"));
        Assert.True(manager.HasTurnState("conv-live"));
        Assert.True(manager.HasHubConnection("conv-live", LiveHubType.Tutor));
        Assert.True(manager.HasConnection("conn-tutor"));

        // Only grammar connection is removed
        Assert.False(manager.HasConnection("conn-grammar"));
        Assert.False(manager.HasHubConnection("conv-live", LiveHubType.Grammar));
    }

    [Fact]
    public async Task GrammarNotifier_ReceivesRealGetGroupTarget_AndRoutesToGrammarGroup()
    {
        string dbName = Guid.NewGuid().ToString();
        var repo = new ConversationRepository(new TestDbFactory(dbName));

        var fakeTutor = new SignalRTestHubContext<TutorConversationHub>();
        var fakeTranscribe = new SignalRTestHubContext<TranscribeLiveHub>();
        var fakeTranslate = new SignalRTestHubContext<TranslateLiveHub>();
        var fakeGrammar = new SignalRTestHubContext<GrammarCorrectionHub>();

        var notifier = new SignalRLiveSessionRealtimeNotifier(
            fakeTutor,
            fakeTranscribe,
            fakeTranslate,
            fakeGrammar);

        var fakeHandler = new FakeHttpMessageHandler(req =>
        {
            var json = "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"{\\\"originalText\\\":\\\"She don't know\\\",\\\"correctedText\\\":\\\"She doesn't know\\\",\\\"errorCategory\\\":\\\"Grammar\\\",\\\"explanationVi\\\":\\\"Ngữ pháp\\\",\\\"cefrTip\\\":\\\"B1\\\"}\"}]}}]}";
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            };
        });

        var (translator, grammar) = CreateFakeAiServices(fakeHandler);
        using var manager = new GeminiLiveConnectionManager(
            notifier: notifier,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            promptBuilder: new CefrPromptBuilder(),
            timeProvider: TimeProvider.System);

        manager.RegisterActiveSessionForTesting("user-a", "conv-real-notifier", "test-api-key", "conn-grammar", LiveHubType.Grammar);

        await manager.AnalyzeTurnGrammarManualAsync("user-a", "conn-grammar", "conv-real-notifier", "turn-1", "She don't know", "B1");

        // Verify that grammar hub clients received the notification at group "grammar:user-a:conv-real-notifier"
        var expectedGroup = "grammar:user-a:conv-real-notifier";
        Assert.True(fakeGrammar.ClientsProxy.GroupProxies.ContainsKey(expectedGroup));

        var invocations = fakeGrammar.ClientsProxy.GroupProxies[expectedGroup].Invocations;
        Assert.Single(invocations);
        Assert.Equal("ReceiveLanguageCorrection", invocations[0].Method);
        var payload = Assert.IsType<LanguageCorrectionDto>(invocations[0].Args[0]);
        Assert.Equal("conv-real-notifier", payload.ConversationId);
        Assert.Equal("turn-1", payload.TurnId);
        Assert.Equal("She doesn't know", payload.CorrectedText);
    }

    [Fact]
    public async Task AnalyzeTurnGrammarManual_WhenAlreadyAnalyzedWithIdenticalText_OrInFlight_SkipsGeminiCall()
    {
        string dbName = Guid.NewGuid().ToString();
        var repo = new ConversationRepository(new TestDbFactory(dbName));

        int geminiCallCount = 0;
        var fakeHandler = new FakeHttpMessageHandler(req =>
        {
            Interlocked.Increment(ref geminiCallCount);
            var json = "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"{\\\"originalText\\\":\\\"She don't know\\\",\\\"correctedText\\\":\\\"She doesn't know\\\",\\\"errorCategory\\\":\\\"Grammar\\\",\\\"explanationVi\\\":\\\"Ngữ pháp\\\",\\\"cefrTip\\\":\\\"B1\\\"}\"}]}}]}";
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            };
        });

        var (translator, grammar) = CreateFakeAiServices(fakeHandler);
        using var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            promptBuilder: new CefrPromptBuilder(),
            timeProvider: TimeProvider.System);

        manager.RegisterActiveSessionForTesting("user-a", "conv-dedup", "key-a", "conn-grammar", LiveHubType.Grammar);

        // 1. First manual call: executes Gemini call
        await manager.AnalyzeTurnGrammarManualAsync("user-a", "conn-grammar", "conv-dedup", "turn-1", "She don't know", "B1");
        Assert.Equal(1, geminiCallCount);

        // 2. Second manual call with identical text (case-insensitive): skipped
        await manager.AnalyzeTurnGrammarManualAsync("user-a", "conn-grammar", "conv-dedup", "turn-1", "she don't know", "B1");
        Assert.Equal(1, geminiCallCount);

        // 3. Different text on the same turn: executes Gemini call
        await manager.AnalyzeTurnGrammarManualAsync("user-a", "conn-grammar", "conv-dedup", "turn-1", "She don't know at all", "B1");
        Assert.Equal(2, geminiCallCount);

        // 4. In-flight simulation: turn added to in-flight set: skipped
        var state = manager.GetTurnState("conv-dedup")!;
        lock (state.SyncRoot)
        {
            state.ManualGrammarInFlightTurns.Add("turn-inflight");
        }
        await manager.AnalyzeTurnGrammarManualAsync("user-a", "conn-grammar", "conv-dedup", "turn-inflight", "New sentence", "B1");
        Assert.Equal(2, geminiCallCount);
    }

    private sealed class FakeFeedbackAudioSynthesizer : IFeedbackAudioSynthesizer
    {
        public int CallCount;
        public TimeSpan Delay = TimeSpan.Zero;
        public string? LastTextSynthesized;
        public string? LastVoice;
        public string? LastAccent;
        public double LastSpeed;
        public string? LastModelCode;

        public async Task<FeedbackAudioResultDto> SynthesizeFeedbackAudioAsync(
            string apiKey,
            string text,
            string voiceName,
            double speechSpeed,
            string accent,
            string? modelCode = null,
            CancellationToken ct = default)
        {
            Interlocked.Increment(ref CallCount);
            LastTextSynthesized = text;
            LastVoice = voiceName;
            LastAccent = accent;
            LastSpeed = speechSpeed;
            LastModelCode = modelCode;

            if (Delay > TimeSpan.Zero)
            {
                await Task.Delay(Delay, ct);
            }

            byte[] bytes = Encoding.UTF8.GetBytes($"audio-for-{text}");
            return new FeedbackAudioResultDto("audio/wav", Convert.ToBase64String(bytes), 1200);
        }
    }

    [Fact]
    public async Task RequestFeedbackAudioAsync_RealManager_UserB_GetsSameAnswerAsMissingSession()
    {
        string dbName = Guid.NewGuid().ToString();
        var repo = new ConversationRepository(new TestDbFactory(dbName));
        var fakeSynthesizer = new FakeFeedbackAudioSynthesizer();
        var (translator, grammar) = CreateFakeAiServices(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        using var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            feedbackAudioSynthesizer: fakeSynthesizer);

        manager.RegisterActiveSessionForTesting("user-a", "conv-audio-1", "test-key", "conn-grammar", LiveHubType.Grammar);

        // A foreign live session must be indistinguishable from a missing one (no probing of other users' sessions).
        var foreign = await Assert.ThrowsAsync<FeedbackAudioException>(async () =>
        {
            await manager.RequestFeedbackAudioAsync("user-b", "conn-grammar", "conv-audio-1", "corr-1", "corrected");
        });
        var missing = await Assert.ThrowsAsync<FeedbackAudioException>(async () =>
        {
            await manager.RequestFeedbackAudioAsync("user-b", "conn-grammar", "conv-does-not-exist", "corr-1", "corrected");
        });
        Assert.Equal("feedback_audio_session_ended", foreign.Code);
        Assert.Equal(missing.Code, foreign.Code);
        Assert.Equal(0, fakeSynthesizer.CallCount);
    }

    [Fact]
    public async Task RequestFeedbackAudioAsync_RealManager_CorrectionOtherConv_ThrowsFeedbackAudioNotFound()
    {
        string dbName = Guid.NewGuid().ToString();
        var repo = new ConversationRepository(new TestDbFactory(dbName));
        var fakeSynthesizer = new FakeFeedbackAudioSynthesizer();
        var (translator, grammar) = CreateFakeAiServices(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        using var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            feedbackAudioSynthesizer: fakeSynthesizer);

        manager.RegisterActiveSessionForTesting("user-a", "conv-audio-1", "test-key", "conn-grammar", LiveHubType.Grammar);

        await repo.SaveCorrectionAsync(new LanguageCorrectionEntity(
            id: "corr-in-other-conv",
            conversationId: "conv-other",
            turnId: "t-1",
            originalText: "bad",
            correctedText: "good",
            errorCategory: "Grammar",
            explanationVi: "Giải thích",
            cefrTip: "Tip"));

        var ex = await Assert.ThrowsAsync<FeedbackAudioException>(async () =>
        {
            await manager.RequestFeedbackAudioAsync("user-a", "conn-grammar", "conv-audio-1", "corr-in-other-conv", "corrected");
        });
        Assert.Equal("feedback_audio_not_found", ex.Code);
        Assert.Equal(0, fakeSynthesizer.CallCount);
    }

    [Fact]
    public async Task RequestFeedbackAudioAsync_RealManager_SecondCallUsesCache_DoesNotCallTtsAgain()
    {
        string dbName = Guid.NewGuid().ToString();
        var repo = new ConversationRepository(new TestDbFactory(dbName));
        var fakeSynthesizer = new FakeFeedbackAudioSynthesizer();
        var (translator, grammar) = CreateFakeAiServices(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        using var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            feedbackAudioSynthesizer: fakeSynthesizer);

        manager.RegisterActiveSessionForTesting("user-a", "conv-audio-2", "test-key", "conn-grammar", LiveHubType.Grammar);

        var state = manager.GetTurnState("conv-audio-2")!;
        state.CorrectionsById["corr-cache"] = new LanguageCorrectionDto(
            Id: "corr-cache",
            ConversationId: "conv-audio-2",
            TurnId: "turn-1",
            OriginalText: "I goes",
            CorrectedText: "I went",
            ErrorCategory: "Grammar",
            ExplanationVi: "Giải thích",
            CefrTip: "Tip",
            Timestamp: DateTimeOffset.UtcNow,
            Status: "has_errors",
            ImprovedText: "I visited"
        );

        var r1 = await manager.RequestFeedbackAudioAsync("user-a", "conn-grammar", "conv-audio-2", "corr-cache", "corrected");
        Assert.NotNull(r1);
        Assert.Equal(1, fakeSynthesizer.CallCount);

        var r2 = await manager.RequestFeedbackAudioAsync("user-a", "conn-grammar", "conv-audio-2", "corr-cache", "corrected");
        Assert.NotNull(r2);
        Assert.Equal(1, fakeSynthesizer.CallCount);
        Assert.Equal(r1.AudioBase64, r2.AudioBase64);
    }

    [Fact]
    public async Task RequestFeedbackAudioAsync_RealManager_TwoConcurrentCalls_CallsTtsOnlyOnce()
    {
        string dbName = Guid.NewGuid().ToString();
        var repo = new ConversationRepository(new TestDbFactory(dbName));
        var fakeSynthesizer = new FakeFeedbackAudioSynthesizer
        {
            Delay = TimeSpan.FromMilliseconds(50)
        };
        var (translator, grammar) = CreateFakeAiServices(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        using var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            feedbackAudioSynthesizer: fakeSynthesizer);

        manager.RegisterActiveSessionForTesting("user-a", "conv-audio-flight", "test-key", "conn-grammar", LiveHubType.Grammar);

        var state = manager.GetTurnState("conv-audio-flight")!;
        state.CorrectionsById["corr-flight"] = new LanguageCorrectionDto(
            Id: "corr-flight",
            ConversationId: "conv-audio-flight",
            TurnId: "turn-1",
            OriginalText: "two month",
            CorrectedText: "two months",
            ErrorCategory: "Grammar",
            ExplanationVi: "Giải thích",
            CefrTip: "Tip",
            Timestamp: DateTimeOffset.UtcNow,
            Status: "has_errors",
            ImprovedText: "two months"
        );

        var task1 = manager.RequestFeedbackAudioAsync("user-a", "conn-grammar", "conv-audio-flight", "corr-flight", "corrected");
        var task2 = manager.RequestFeedbackAudioAsync("user-a", "conn-grammar", "conv-audio-flight", "corr-flight", "corrected");

        var results = await Task.WhenAll(task1, task2);
        Assert.Equal(1, fakeSynthesizer.CallCount);
        Assert.Equal(results[0].AudioBase64, results[1].AudioBase64);
    }

    [Fact]
    public async Task RequestFeedbackAudioAsync_RealManager_SessionEnded_ThrowsFeedbackAudioSessionEnded()
    {
        string dbName = Guid.NewGuid().ToString();
        var repo = new ConversationRepository(new TestDbFactory(dbName));
        var fakeSynthesizer = new FakeFeedbackAudioSynthesizer();
        var (translator, grammar) = CreateFakeAiServices(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        using var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            feedbackAudioSynthesizer: fakeSynthesizer);

        var ex = await Assert.ThrowsAsync<FeedbackAudioException>(async () =>
        {
            await manager.RequestFeedbackAudioAsync("user-a", "conn-grammar", "conv-ended", "corr-1", "corrected");
        });
        Assert.Equal("feedback_audio_session_ended", ex.Code);
    }

    [Fact]
    public async Task RequestFeedbackAudioAsync_RealManager_CacheEvictsOldestWhenExceeds30()
    {
        string dbName = Guid.NewGuid().ToString();
        var repo = new ConversationRepository(new TestDbFactory(dbName));
        var fakeSynthesizer = new FakeFeedbackAudioSynthesizer();
        var (translator, grammar) = CreateFakeAiServices(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        using var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar,
            feedbackAudioSynthesizer: fakeSynthesizer);

        manager.RegisterActiveSessionForTesting("user-a", "conv-audio-lru", "test-key", "conn-grammar", LiveHubType.Grammar);
        var state = manager.GetTurnState("conv-audio-lru")!;

        for (int i = 1; i <= 31; i++)
        {
            state.CorrectionsById[$"corr-{i}"] = new LanguageCorrectionDto(
                Id: $"corr-{i}",
                ConversationId: "conv-audio-lru",
                TurnId: $"turn-{i}",
                OriginalText: $"text {i}",
                CorrectedText: $"corrected {i}",
                ErrorCategory: "Grammar",
                ExplanationVi: "Giải thích",
                CefrTip: "Tip",
                Timestamp: DateTimeOffset.UtcNow,
                Status: "has_errors",
                ImprovedText: $"improved {i}"
            );
        }

        for (int i = 1; i <= 30; i++)
        {
            await manager.RequestFeedbackAudioAsync("user-a", "conn-grammar", "conv-audio-lru", $"corr-{i}", "corrected");
        }
        Assert.Equal(30, fakeSynthesizer.CallCount);

        await manager.RequestFeedbackAudioAsync("user-a", "conn-grammar", "conv-audio-lru", "corr-31", "corrected");
        Assert.Equal(31, fakeSynthesizer.CallCount);

        // corr-1 was evicted, so calling it again triggers synthesizer
        await manager.RequestFeedbackAudioAsync("user-a", "conn-grammar", "conv-audio-lru", "corr-1", "corrected");
        Assert.Equal(32, fakeSynthesizer.CallCount);

        // corr-31 is still in cache, so calling it again does NOT trigger synthesizer
        await manager.RequestFeedbackAudioAsync("user-a", "conn-grammar", "conv-audio-lru", "corr-31", "corrected");
        Assert.Equal(32, fakeSynthesizer.CallCount);
    }

    [Fact]
    public async Task BuildRecentContext_ChainedTurns_ContainsTeacherSpeechAndTruncatesAt2000()
    {
        string dbName = Guid.NewGuid().ToString();
        var repo = new ConversationRepository(new TestDbFactory(dbName));
        string? capturedRecentContext = null;

        var fakeHandler = new FakeHttpMessageHandler(req =>
        {
            var body = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            var jsonNode = JsonNode.Parse(body ?? "{}");
            var prompt = jsonNode?["contents"]?[0]?["parts"]?[0]?["text"]?.GetValue<string>();
            capturedRecentContext = prompt;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"{\\\"status\\\":\\\"has_errors\\\",\\\"originalText\\\":\\\"câu\\\",\\\"correctedText\\\":\\\"câu tốt\\\",\\\"errorCategory\\\":\\\"Grammar\\\",\\\"explanationVi\\\":\\\"e\\\",\\\"cefrTip\\\":\\\"t\\\"}\"}]}}]}", Encoding.UTF8, "application/json")
            };
        });

        var (translator, grammar) = CreateFakeAiServices(fakeHandler);
        using var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar);

        manager.RegisterActiveSessionForTesting("user-a", "conv-context", "test-key", "conn-grammar", LiveHubType.Grammar);
        var state = manager.GetTurnState("conv-context")!;

        // 1. Teacher turn 1
        state.TranscriptByTurn["t-ai-1"] = "Hello! What did you do today?";
        await manager.FinalizeTurnAsync("conv-context", state, "ai", "t-ai-1", 1);

        // 2. Student turn 1
        state.TranscriptByTurn["t-u-1"] = "I went to school.";
        await manager.FinalizeTurnAsync("conv-context", state, "user", "t-u-1", 2);

        // 3. Teacher turn 2
        state.TranscriptByTurn["t-ai-2"] = "Did you learn something interesting?";
        await manager.FinalizeTurnAsync("conv-context", state, "ai", "t-ai-2", 3);

        // 4. Student turn 2 (triggers grammar check)
        state.TranscriptByTurn["t-u-2"] = "I learn many new informations.";
        await manager.FinalizeTurnAsync("conv-context", state, "user", "t-u-2", 4);

        var completedTcs = new TaskCompletionSource<bool>();
        manager.OnGrammarCheckCompleted = () => completedTcs.TrySetResult(true);
        await Task.WhenAny(completedTcs.Task, Task.Delay(500));

        Assert.NotNull(capturedRecentContext);
        Assert.Contains("Did you learn something interesting?", capturedRecentContext);
        Assert.Contains("Teacher:", capturedRecentContext);
        Assert.Contains("Student:", capturedRecentContext);
    }

    [Fact]
    public async Task AnalyzeTurnGrammar_WhenAnalysisFails_DoesNotPersistToDb_AndDoesNotRecordLastAnalyzedText()
    {
        string dbName = Guid.NewGuid().ToString();
        var repo = new ConversationRepository(new TestDbFactory(dbName));
        await repo.CreateConversationAsync("user-a", "conv-failed", "Conv Failed", "B1", false, "Aoede", 1.0);

        var fakeHandler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var (translator, grammar) = CreateFakeAiServices(fakeHandler);

        using var manager = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator,
            grammarAnalyzer: grammar);

        manager.RegisterActiveSessionForTesting("user-a", "conv-failed", "test-key", "conn-grammar", LiveHubType.Grammar);
        var state = manager.GetTurnState("conv-failed")!;

        string text = "This sentence fails AI analysis completely and has no deterministic regex.";
        await manager.AnalyzeTurnGrammarManualAsync("user-a", "conn-grammar", "conv-failed", "turn-fail", text, "B1");

        // 1. LastAnalyzedTextByTurn must NOT record this turn
        Assert.False(state.LastAnalyzedTextByTurn.ContainsKey("turn-fail"));

        // 2. DB has NO correction saved for this turn
        var dbDetail = await repo.GetConversationDetailAsync("user-a", "conv-failed");
        Assert.NotNull(dbDetail);
        Assert.Empty(dbDetail.Corrections);

        // 3. User clicks "Phân tích lại": re-analysis of identical text is permitted
        int reanalyzeCalls = 0;
        var fakeHandler2 = new FakeHttpMessageHandler(_ =>
        {
            Interlocked.Increment(ref reanalyzeCalls);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"{\\\"status\\\":\\\"already_natural\\\",\\\"originalText\\\":\\\"" + text + "\\\",\\\"correctedText\\\":\\\"\\\",\\\"improvedText\\\":\\\"\\\",\\\"errorCategory\\\":\\\"None\\\",\\\"explanationVi\\\":\\\"OK\\\",\\\"cefrTip\\\":\\\"Tip\\\",\\\"issues\\\":[]}\"}]}}]}", Encoding.UTF8, "application/json")
            };
        });
        var (translator2, grammar2) = CreateFakeAiServices(fakeHandler2);
        using var manager2 = new GeminiLiveConnectionManager(
            notifier: NullLiveSessionRealtimeNotifier.Instance,
            repo: repo,
            translator: translator2,
            grammarAnalyzer: grammar2);
        manager2.RegisterActiveSessionForTesting("user-a", "conv-failed-2", "test-key", "conn-grammar", LiveHubType.Grammar);

        await manager2.AnalyzeTurnGrammarManualAsync("user-a", "conn-grammar", "conv-failed-2", "turn-fail", text, "B1");
        Assert.Equal(1, reanalyzeCalls);
    }

}