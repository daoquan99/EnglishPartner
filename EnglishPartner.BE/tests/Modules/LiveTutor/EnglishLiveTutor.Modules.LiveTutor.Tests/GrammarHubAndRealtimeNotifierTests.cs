using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Claims;
using EnglishLiveTutor.Api.Hubs;
using EnglishLiveTutor.Api.Realtime;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Conversations.Ports;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Xunit;

namespace EnglishLiveTutor.Modules.LiveTutor.Tests;

public class GrammarHubAndRealtimeNotifierTests
{
    // =========================================================================
    // Test Doubles for SignalR
    // =========================================================================

    private sealed class FakeHubCallerContext : HubCallerContext
    {
        public FakeHubCallerContext(string connectionId, ClaimsPrincipal user)
        {
            ConnectionId = connectionId;
            User = user;
        }

        public override string ConnectionId { get; }
        public override string? UserIdentifier => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        public override ClaimsPrincipal? User { get; }
        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
        public override IFeatureCollection Features { get; } = new FeatureCollection();
        public override CancellationToken ConnectionAborted => CancellationToken.None;
        public override void Abort() { }
    }

    private sealed class FakeGroupManager : IGroupManager
    {
        public List<(string ConnectionId, string GroupName)> AddedToGroup { get; } = new();
        public List<(string ConnectionId, string GroupName)> RemovedFromGroup { get; } = new();

        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            AddedToGroup.Add((connectionId, groupName));
            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            RemovedFromGroup.Add((connectionId, groupName));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeClientProxy : ISingleClientProxy
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

    private sealed class FakeHubCallerClients : IHubCallerClients
    {
        public FakeClientProxy CallerProxy { get; } = new();

        public ISingleClientProxy Caller => CallerProxy;
        IClientProxy IHubCallerClients<IClientProxy>.Caller => CallerProxy;

        public IClientProxy All => throw new NotImplementedException();
        public IClientProxy Others => throw new NotImplementedException();
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotImplementedException();
        public IClientProxy Client(string connectionId) => throw new NotImplementedException();
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotImplementedException();
        public IClientProxy Group(string groupName) => throw new NotImplementedException();
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotImplementedException();
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => throw new NotImplementedException();
        public IClientProxy OthersInGroup(string groupName) => throw new NotImplementedException();
        public IClientProxy User(string userId) => throw new NotImplementedException();
        public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotImplementedException();
    }

    private sealed class FakeHubClients : IHubClients
    {
        public ConcurrentDictionary<string, FakeClientProxy> GroupProxies { get; } = new();

        public IClientProxy Group(string groupName) =>
            GroupProxies.GetOrAdd(groupName, _ => new FakeClientProxy());

        public IClientProxy All => throw new NotImplementedException();
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotImplementedException();
        public IClientProxy Client(string connectionId) => throw new NotImplementedException();
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotImplementedException();
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotImplementedException();
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => throw new NotImplementedException();
        public IClientProxy User(string userId) => throw new NotImplementedException();
        public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotImplementedException();
    }

    private sealed class FakeHubContext<THub> : IHubContext<THub> where THub : Hub
    {
        public FakeHubClients ClientsProxy { get; } = new();
        public FakeGroupManager GroupsManager { get; } = new();

        public IHubClients Clients => ClientsProxy;
        public IGroupManager Groups => GroupsManager;
    }

    private sealed class FakeLiveSessionService : ILiveSessionService
    {
        public string? OwnerUserId { get; set; } = "user-a";
        public string? ActiveConversationId { get; set; } = "conv-1";

        public int StartGrammarSessionCalls { get; private set; }
        public int AnalyzeTurnGrammarCalls { get; private set; }

        public Task StartGrammarHubSessionAsync(string userId, string connectionId, StartSubHubSessionRequest request)
        {
            StartGrammarSessionCalls++;
            if (!string.Equals(userId, OwnerUserId, StringComparison.Ordinal))
            {
                throw new NotFoundException("Không tìm thấy hội thoại.");
            }
            return Task.CompletedTask;
        }

        public Task AnalyzeTurnGrammarManualAsync(string userId, string connectionId, string conversationId, string turnId, string transcriptText, string cefrLevel, CancellationToken ct = default)
        {
            AnalyzeTurnGrammarCalls++;
            if (!string.Equals(userId, OwnerUserId, StringComparison.Ordinal))
            {
                throw new NotFoundException("Không tìm thấy hội thoại.");
            }
            return Task.CompletedTask;
        }

        public Task StartTutorSessionAsync(string userId, string connectionId, StartTutorSessionRequest request) => Task.CompletedTask;
        public Task ForwardTutorHubAudioAsync(string userId, string connectionId, string conversationId, string base64Pcm16k) => Task.CompletedTask;
        public Task EndUserSpeechAsync(string userId, string connectionId, string conversationId) => Task.CompletedTask;
        public Task StopAllForConversationAsync(string userId, string connectionId, string conversationId) => Task.CompletedTask;
        public Task StartTranscribeHubSessionAsync(string userId, string connectionId, StartSubHubSessionRequest request) => Task.CompletedTask;
        public Task ForwardTranscribeHubAudioAsync(string userId, string connectionId, string conversationId, string base64Pcm16k) => Task.CompletedTask;
        public Task StartTranslateHubSessionAsync(string userId, string connectionId, StartSubHubSessionRequest request) => Task.CompletedTask;
        public Task ForwardTranslateHubAudioAsync(string userId, string connectionId, string conversationId, string base64Pcm16k) => Task.CompletedTask;
        public Task EndTranslateSpeechAsync(string userId, string connectionId, string conversationId) => Task.CompletedTask;
        public int RequestFeedbackAudioCalls { get; private set; }
        public Task<FeedbackAudioResultDto> RequestFeedbackAudioAsync(
            string userId,
            string connectionId,
            string conversationId,
            string correctionId,
            string variant,
            CancellationToken ct = default)
        {
            RequestFeedbackAudioCalls++;
            if (!string.Equals(userId, OwnerUserId, StringComparison.Ordinal))
            {
                throw new NotFoundException("Không tìm thấy hội thoại.");
            }
            return Task.FromResult(new FeedbackAudioResultDto("audio/wav", "dummy-audio", 1200));
        }
        public Task OnClientDisconnectedAsync(string connectionId) => Task.CompletedTask;
    }

    private static GrammarCorrectionHub CreateGrammarHub(
        ILiveSessionService sessionService,
        string userId,
        string connectionId,
        out FakeGroupManager groupManager,
        out FakeHubCallerClients callerClients,
        IGrammarHubRateLimiter? rateLimiter = null)
    {
        groupManager = new FakeGroupManager();
        callerClients = new FakeHubCallerClients();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        var context = new FakeHubCallerContext(connectionId, principal);

        var hub = new GrammarCorrectionHub(sessionService, rateLimiter ?? new GrammarHubRateLimiter())
        {
            Context = context,
            Groups = groupManager,
            Clients = callerClients
        };

        return hub;
    }

    // =========================================================================
    // 1. InitializeGrammarSession Tests (Owner vs Cross-User)
    // =========================================================================

    [Fact]
    public async Task InitializeGrammarSession_WhenCallerIsOwner_JoinsGrammarGroupAndEmitsReady()
    {
        var sessionService = new FakeLiveSessionService { OwnerUserId = "user-a" };
        var hub = CreateGrammarHub(sessionService, "user-a", "conn-123", out var groupManager, out var callerClients);

        await hub.InitializeGrammarSession(new StartSubHubSessionRequest("conv-1", "test-api-key"));

        Assert.Equal(1, sessionService.StartGrammarSessionCalls);
        Assert.Single(groupManager.AddedToGroup);
        Assert.Equal(("conn-123", "grammar:user-a:conv-1"), groupManager.AddedToGroup[0]);

        Assert.Single(callerClients.CallerProxy.Invocations);
        Assert.Equal("GrammarHubReady", callerClients.CallerProxy.Invocations[0].Method);
    }

    [Fact]
    public async Task InitializeGrammarSession_WhenCallerIsNotOwner_ThrowsHubException_AndDoesNotJoinGroup()
    {
        var sessionService = new FakeLiveSessionService { OwnerUserId = "user-a" };
        var hub = CreateGrammarHub(sessionService, "user-b", "conn-456", out var groupManager, out var callerClients);

        var ex = await Assert.ThrowsAsync<HubException>(() =>
            hub.InitializeGrammarSession(new StartSubHubSessionRequest("conv-1", "test-api-key")));

        Assert.Equal("Không tìm thấy hội thoại.", ex.Message);
        Assert.Empty(groupManager.AddedToGroup);
        Assert.Empty(callerClients.CallerProxy.Invocations);
    }

    [Fact]
    public async Task InitializeGrammarSession_WhenRateLimitExceeded_BlocksRequestsAfterThreshold()
    {
        var sessionService = new FakeLiveSessionService { OwnerUserId = "user-a" };
        var rateLimiter = new GrammarHubRateLimiter(limit: 3, window: TimeSpan.FromMinutes(1));
        var hub = CreateGrammarHub(sessionService, "user-a", "conn-123", out _, out _, rateLimiter);

        // 3 calls within limit succeed
        await hub.InitializeGrammarSession(new StartSubHubSessionRequest("conv-1", "test-api-key"));
        await hub.InitializeGrammarSession(new StartSubHubSessionRequest("conv-1", "test-api-key"));
        await hub.InitializeGrammarSession(new StartSubHubSessionRequest("conv-1", "test-api-key"));

        Assert.Equal(3, sessionService.StartGrammarSessionCalls);

        // 4th call exceeds threshold and throws HubException
        var ex = await Assert.ThrowsAsync<HubException>(() =>
            hub.InitializeGrammarSession(new StartSubHubSessionRequest("conv-1", "test-api-key")));

        Assert.Contains("quá nhiều yêu cầu", ex.Message);
        Assert.Equal(3, sessionService.StartGrammarSessionCalls);
    }

    // =========================================================================
    // 2. AnalyzeTurnGrammar Tests (Ownership, Input Validation, Rate Limiting)
    // =========================================================================

    [Fact]
    public async Task AnalyzeTurnGrammar_WhenCallerIsNotOwner_ThrowsHubException()
    {
        var sessionService = new FakeLiveSessionService { OwnerUserId = "user-a" };
        var hub = CreateGrammarHub(sessionService, "user-b", "conn-456", out _, out _);

        var ex = await Assert.ThrowsAsync<HubException>(() =>
            hub.AnalyzeTurnGrammar("conv-1", "turn-1", "Hello world", "B1"));

        Assert.Equal("Không tìm thấy hội thoại.", ex.Message);
    }

    [Fact]
    public async Task AnalyzeTurnGrammar_WhenTranscriptTextExceeds2000Characters_RejectsWithHubException()
    {
        var sessionService = new FakeLiveSessionService { OwnerUserId = "user-a" };
        var hub = CreateGrammarHub(sessionService, "user-a", "conn-123", out _, out _);
        var longTranscript = new string('a', 2001);

        var ex = await Assert.ThrowsAsync<HubException>(() =>
            hub.AnalyzeTurnGrammar("conv-1", "turn-1", longTranscript, "B1"));

        Assert.Contains("transcriptText không hợp lệ hoặc vượt quá 2000 ký tự", ex.Message);
        Assert.Equal(0, sessionService.AnalyzeTurnGrammarCalls);
    }

    [Fact]
    public async Task AnalyzeTurnGrammar_WhenTurnIdExceeds64Characters_RejectsWithHubException()
    {
        var sessionService = new FakeLiveSessionService { OwnerUserId = "user-a" };
        var hub = CreateGrammarHub(sessionService, "user-a", "conn-123", out _, out _);
        var longTurnId = new string('t', 65);

        var ex = await Assert.ThrowsAsync<HubException>(() =>
            hub.AnalyzeTurnGrammar("conv-1", longTurnId, "Hello world", "B1"));

        Assert.Contains("turnId không hợp lệ hoặc vượt quá 64 ký tự", ex.Message);
        Assert.Equal(0, sessionService.AnalyzeTurnGrammarCalls);
    }

    [Theory]
    [InlineData("D1")]
    [InlineData("Z2")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AnalyzeTurnGrammar_WhenCefrLevelIsInvalid_RejectsWithHubException(string invalidCefr)
    {
        var sessionService = new FakeLiveSessionService { OwnerUserId = "user-a" };
        var hub = CreateGrammarHub(sessionService, "user-a", "conn-123", out _, out _);

        var ex = await Assert.ThrowsAsync<HubException>(() =>
            hub.AnalyzeTurnGrammar("conv-1", "turn-1", "Hello world", invalidCefr));

        Assert.Contains("cefrLevel không hợp lệ", ex.Message);
        Assert.Equal(0, sessionService.AnalyzeTurnGrammarCalls);
    }

    [Fact]
    public async Task AnalyzeTurnGrammar_WhenRateLimitExceeded_BlocksRequestsAfterThreshold()
    {
        var sessionService = new FakeLiveSessionService { OwnerUserId = "user-a" };
        var rateLimiter = new GrammarHubRateLimiter(limit: 3, window: TimeSpan.FromMinutes(1));
        var hub = CreateGrammarHub(sessionService, "user-a", "conn-123", out _, out _, rateLimiter);

        // 3 calls within limit succeed
        await hub.AnalyzeTurnGrammar("conv-1", "turn-1", "Sentence 1", "B1");
        await hub.AnalyzeTurnGrammar("conv-1", "turn-2", "Sentence 2", "B1");
        await hub.AnalyzeTurnGrammar("conv-1", "turn-3", "Sentence 3", "B1");

        Assert.Equal(3, sessionService.AnalyzeTurnGrammarCalls);

        // 4th call exceeds threshold and throws HubException
        var ex = await Assert.ThrowsAsync<HubException>(() =>
            hub.AnalyzeTurnGrammar("conv-1", "turn-4", "Sentence 4", "B1"));

        Assert.Contains("quá nhiều yêu cầu", ex.Message);
        Assert.Equal(3, sessionService.AnalyzeTurnGrammarCalls);
    }

    // =========================================================================
    // 3. SignalRLiveSessionRealtimeNotifier Tests
    // =========================================================================

    [Fact]
    public async Task SendLanguageCorrectionAsync_SendsToGrammarHubGroup_AndNotToTutorHub()
    {
        var tutorHub = new FakeHubContext<TutorConversationHub>();
        var transcribeHub = new FakeHubContext<TranscribeLiveHub>();
        var translateHub = new FakeHubContext<TranslateLiveHub>();
        var grammarHub = new FakeHubContext<GrammarCorrectionHub>();

        var notifier = new SignalRLiveSessionRealtimeNotifier(
            tutorHub,
            transcribeHub,
            translateHub,
            grammarHub);

        var correction = new LanguageCorrectionDto(
            Id: "corr-1",
            ConversationId: "conv-1",
            TurnId: "turn-1",
            OriginalText: "I goes to school.",
            CorrectedText: "I go to school.",
            ErrorCategory: "Subject-Verb Agreement",
            ExplanationVi: "Chủ ngữ 'I' đi với 'go', không dùng 'goes'.",
            CefrTip: "Ôn lại thì hiện tại đơn.",
            Timestamp: DateTimeOffset.UtcNow);

        await notifier.SendLanguageCorrectionAsync("user-a:conv-1", correction);

        // Assert grammarHub received the event on group "grammar:user-a:conv-1"
        var expectedGroup = "grammar:user-a:conv-1";
        Assert.True(grammarHub.ClientsProxy.GroupProxies.ContainsKey(expectedGroup));
        var grammarInvocations = grammarHub.ClientsProxy.GroupProxies[expectedGroup].Invocations;
        Assert.Single(grammarInvocations);
        Assert.Equal("ReceiveLanguageCorrection", grammarInvocations[0].Method);
        Assert.Equal(correction, grammarInvocations[0].Args[0]);

        // Assert tutorHub received ZERO invocations
        Assert.Empty(tutorHub.ClientsProxy.GroupProxies);
    }

    // =========================================================================
    // 4. TutorConversationHub Reflection Test
    // =========================================================================

    [Fact]
    public void TutorConversationHub_DoesNotContain_AnalyzeTurnGrammar_Method()
    {
        var method = typeof(TutorConversationHub).GetMethod(
            "AnalyzeTurnGrammar",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.Null(method);
    }

    // =========================================================================
    // 5. RequestFeedbackAudio Tests
    // =========================================================================

    [Theory]
    [InlineData("corrected")]
    [InlineData("improved")]
    public async Task RequestFeedbackAudio_WhenUserIsOwner_ReturnsAudioResult(string variant)
    {
        var sessionService = new FakeLiveSessionService { OwnerUserId = "user-a" };
        var hub = CreateGrammarHub(sessionService, "user-a", "conn-123", out _, out _);

        var result = await hub.RequestFeedbackAudio("conv-1", "corr-1", variant);

        Assert.NotNull(result);
        Assert.Equal("audio/wav", result.MimeType);
        Assert.Equal("dummy-audio", result.AudioBase64);
        Assert.Equal(1, sessionService.RequestFeedbackAudioCalls);
    }

    [Fact]
    public async Task RequestFeedbackAudio_WhenUserIsNotOwner_RejectsWithHubException()
    {
        var sessionService = new FakeLiveSessionService { OwnerUserId = "user-a" };
        var hub = CreateGrammarHub(sessionService, "user-b", "conn-123", out _, out _);

        var ex = await Assert.ThrowsAsync<HubException>(() =>
            hub.RequestFeedbackAudio("conv-1", "corr-1", "corrected"));

        Assert.Contains("Không tìm thấy hội thoại", ex.Message);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData("other")]
    public async Task RequestFeedbackAudio_WhenVariantIsInvalid_RejectsWithHubException(string invalidVariant)
    {
        var sessionService = new FakeLiveSessionService { OwnerUserId = "user-a" };
        var hub = CreateGrammarHub(sessionService, "user-a", "conn-123", out _, out _);

        var ex = await Assert.ThrowsAsync<HubException>(() =>
            hub.RequestFeedbackAudio("conv-1", "corr-1", invalidVariant));

        Assert.Contains("variant", ex.Message);
        Assert.Equal(0, sessionService.RequestFeedbackAudioCalls);
    }

    [Fact]
    public async Task RequestFeedbackAudio_WhenRateLimitExceeded_BlocksRequestsAfterThreshold()
    {
        var sessionService = new FakeLiveSessionService { OwnerUserId = "user-a" };
        var rateLimiter = new GrammarHubRateLimiter(limit: 2, window: TimeSpan.FromMinutes(1));
        var hub = CreateGrammarHub(sessionService, "user-a", "conn-123", out _, out _, rateLimiter);

        // 2 calls within limit succeed
        await hub.RequestFeedbackAudio("conv-1", "corr-1", "corrected");
        await hub.RequestFeedbackAudio("conv-1", "corr-2", "improved");

        Assert.Equal(2, sessionService.RequestFeedbackAudioCalls);

        // 3rd call exceeds threshold
        var ex = await Assert.ThrowsAsync<HubException>(() =>
            hub.RequestFeedbackAudio("conv-1", "corr-3", "corrected"));

        Assert.Contains("quá nhiều yêu cầu", ex.Message);
        Assert.Equal(2, sessionService.RequestFeedbackAudioCalls);
    }
}
