using System.Collections.Concurrent;
using System.Text;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Conversations.Ports;
using EnglishLiveTutor.Application.Features.Conversations.Validation;
using EnglishLiveTutor.Domain.Conversations;
using EnglishLiveTutor.Domain.Conversations.Entities;
using EnglishLiveTutor.Domain.Conversations.Repositories;
using EnglishLiveTutor.Infrastructure.Ai;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishLiveTutor.Infrastructure.Realtime;

public sealed class ConversationTurnState
{
    public object SyncRoot { get; } = new();
    public int LastSequenceOrder { get; set; }
    public string CefrLevel { get; set; } = "B1";

    public string? ActiveUserTurnId { get; set; }
    public int ActiveUserSequence { get; set; }
    public bool UserTurnHasText { get; set; }

    public string? ActiveAiTurnId { get; set; }
    public int ActiveAiSequence { get; set; }
    public bool AiIsSpeaking { get; set; }

    public Dictionary<string, string> TranscriptByTurn { get; } = new();
    public Dictionary<string, string> TutorSourceTranscriptByTurn { get; } = new();
    public Dictionary<string, string> TranscribeSourceTranscriptByTurn { get; } = new();
    public Dictionary<string, string> TranslationByTurn { get; } = new();
    public Dictionary<string, string> AudioTranslationBufferByTurn { get; } = new();
    public Dictionary<string, int> SequenceByTurn { get; } = new();
    public Dictionary<string, string> LastAnalyzedTextByTurn { get; } = new();
    public Dictionary<string, CancellationTokenSource> TranslationDebounceByTurn { get; } = new();
    public Dictionary<string, CancellationTokenSource> GrammarDebounceByTurn { get; } = new();
    public Dictionary<string, CancellationTokenSource> GrammarFinalCtsByTurn { get; } = new();
    public HashSet<string> ManualGrammarInFlightTurns { get; } = new();

    // Throttled persistence tracking
    public Dictionary<string, DateTimeOffset> LastPersistedTranscriptAtByTurn { get; } = new();
    public Dictionary<string, (string Speaker, int Seq, string Text)> PendingTranscriptByTurn { get; } = new();
    public Dictionary<string, CancellationTokenSource> TranscriptThrottleCtsByTurn { get; } = new();

    public Dictionary<string, DateTimeOffset> LastPersistedTranslationAtByTurn { get; } = new();
    public Dictionary<string, (string Speaker, int Seq, string Text)> PendingTranslationByTurn { get; } = new();
    public Dictionary<string, CancellationTokenSource> TranslationThrottleCtsByTurn { get; } = new();

    // Final flags per turn to ensure final always wins
    public HashSet<string> FinalTranscriptTurns { get; } = new();
    public HashSet<string> FinalTranslationTurns { get; } = new();

    // Per-turn serialization for transcript writes to guarantee order without DB race
    public ConcurrentDictionary<string, SemaphoreSlim> TranscriptSemaphoresByTurn { get; } = new();

    // Per-turn serialization for translation writes to guarantee order without DB IsFinal column
    public ConcurrentDictionary<string, SemaphoreSlim> TranslationSemaphoresByTurn { get; } = new();

    public bool IsDisposed { get; set; }

    public string ConversationMode { get; set; } = "daily_conversation";
    public string Topic { get; set; } = string.Empty;
    public string VoiceName { get; set; } = "Aoede";
    public double SpeechSpeed { get; set; } = 1.0;
    public string Accent { get; set; } = "US";
    public string TtsModelCode { get; set; } = "gemini-3.8-live";
    public ConcurrentDictionary<string, LanguageCorrectionDto> CorrectionsById { get; } = new();

    // Cache byte[] with LRU max 30 entries & single-flight
    public ConcurrentDictionary<(string CorrectionId, string Variant), Lazy<Task<CachedFeedbackAudio>>> FeedbackAudioFlight { get; } = new();
    public LinkedList<(string CorrectionId, string Variant)> FeedbackAudioLruOrder { get; } = new();
    public object FeedbackAudioLock { get; } = new();

    // Context buffer of finalized turns (speaker and full text, max 12)
    public List<(string Speaker, string Text)> RecentFinalizedTurns { get; } = new();
}

public sealed record CachedFeedbackAudio(string MimeType, byte[] AudioBytes, int DurationMs);

public sealed class GeminiLiveConnectionManager : ILiveSessionService, IDisposable
{
    private const string TranscribeLiveModel = "models/gemini-3.5-transcribe-live";
    private const string TranslateLiveModel = "models/gemini-3.5-live-translate-preview";

    private readonly ILiveSessionRealtimeNotifier _notifier;
    private readonly IConversationRepository _repo;
    private readonly FastTextTranslatorFallback _translator;
    private readonly GrammarCorrectionAnalyzerService _grammarAnalyzer;
    private readonly ICefrPromptBuilder _promptBuilder;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<GeminiLiveConnectionManager>? _logger;
    private readonly IFeedbackAudioSynthesizer? _feedbackAudioSynthesizer;
    private readonly int _idleTimeoutMinutes;
    private readonly ITimer _idleCheckTimer;
    private int _isSweeping;

    private readonly ConcurrentDictionary<string, int> _initializingStartsByConversation = new();
    private readonly ConcurrentDictionary<string, GeminiLiveSessionBridge> _bridges = new();
    private readonly ConcurrentDictionary<string, string> _sessionApiKeys = new();
    private readonly ConcurrentDictionary<string, ConversationTurnState> _turnStates = new();
    private readonly ConcurrentDictionary<string, string> _sessionOwners = new();
    private readonly ConcurrentDictionary<string, LiveConnectionInfo> _connections = new();
    private readonly ConcurrentDictionary<(string ConversationId, LiveHubType HubType), string> _hubConnections = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastActivityByConversation = new();

    internal Func<string, string, GeminiLiveSessionBridge>? BridgeFactory { get; set; }
    internal Action<string, bool>? OnWaitingForTurnSemaphore { get; set; }
    internal Action? OnGrammarCheckCompleted { get; set; }

    public GeminiLiveConnectionManager(
        ILiveSessionRealtimeNotifier notifier,
        IConversationRepository repo,
        FastTextTranslatorFallback translator,
        GrammarCorrectionAnalyzerService grammarAnalyzer,
        ICefrPromptBuilder? promptBuilder = null,
        TimeProvider? timeProvider = null,
        IOptions<LiveSessionOptions>? options = null,
        ILogger<GeminiLiveConnectionManager>? logger = null,
        IFeedbackAudioSynthesizer? feedbackAudioSynthesizer = null)
    {
        _notifier = notifier;
        _repo = repo;
        _translator = translator;
        _grammarAnalyzer = grammarAnalyzer;
        _promptBuilder = promptBuilder ?? new CefrPromptBuilder();
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger;
        _feedbackAudioSynthesizer = feedbackAudioSynthesizer;
        _idleTimeoutMinutes = options?.Value?.IdleTimeoutMinutes > 0 ? options.Value.IdleTimeoutMinutes : 15;

        _idleCheckTimer = _timeProvider.CreateTimer(
            _ =>
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await CleanupIdleSessionsAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning("Lỗi khi chạy định kỳ dọn phiên rảnh: {ExceptionType}", ex.GetType().Name);
                    }
                });
            },
            null,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(1));
    }

    private void MarkConversationStartBegin(string conversationId)
    {
        _initializingStartsByConversation.AddOrUpdate(conversationId, 1, (_, count) => count + 1);
    }

    private void MarkConversationStartEnd(string conversationId)
    {
        _initializingStartsByConversation.AddOrUpdate(conversationId, 0, (_, count) => Math.Max(0, count - 1));
        if (_initializingStartsByConversation.TryGetValue(conversationId, out var count) && count == 0)
        {
            _initializingStartsByConversation.TryRemove(new KeyValuePair<string, int>(conversationId, 0));
        }
    }

    private bool IsConversationInitializing(string conversationId)
    {
        return _initializingStartsByConversation.TryGetValue(conversationId, out var count) && count > 0;
    }

    private async Task RollbackStartFailureAsync(
        string conversationId,
        string connectionId,
        LiveHubType hubType,
        GeminiLiveSessionBridge? bridge)
    {
        if (bridge is not null)
        {
            try { await bridge.DisposeAsync(); } catch { }
        }

        string bridgeKey = $"{conversationId}:{hubType.ToString().ToLowerInvariant()}";
        if (bridge is not null)
        {
            // Only remove the bridge this Start registered; a newer Start on a replacement connection may own the key now.
            _bridges.TryRemove(new KeyValuePair<string, GeminiLiveSessionBridge>(bridgeKey, bridge));
        }
        _connections.TryRemove(connectionId, out _);
        _hubConnections.TryRemove(new KeyValuePair<(string, LiveHubType), string>((conversationId, hubType), connectionId));

        bool otherHubsActive = _bridges.Keys.Any(k => k.StartsWith($"{conversationId}:", StringComparison.Ordinal)) ||
                               _connections.Values.Any(c => string.Equals(c.ConversationId, conversationId, StringComparison.Ordinal)) ||
                               _hubConnections.Keys.Any(k => string.Equals(k.ConversationId, conversationId, StringComparison.Ordinal));

        bool hasOtherInitializingStarts = _initializingStartsByConversation.TryGetValue(conversationId, out var count) && count > 1;

        if (!otherHubsActive && !hasOtherInitializingStarts)
        {
            _sessionOwners.TryRemove(conversationId, out _);
            _sessionApiKeys.TryRemove(conversationId, out _);
            _lastActivityByConversation.TryRemove(conversationId, out _);
            if (_turnStates.TryRemove(conversationId, out var state))
            {
                lock (state.SyncRoot)
                {
                    state.IsDisposed = true;
                    foreach (var (_, cts) in state.TranslationDebounceByTurn) try { cts.Cancel(); cts.Dispose(); } catch { }
                    state.TranslationDebounceByTurn.Clear();
                    foreach (var (_, cts) in state.GrammarDebounceByTurn) try { cts.Cancel(); cts.Dispose(); } catch { }
                    state.GrammarDebounceByTurn.Clear();
                    foreach (var (_, cts) in state.GrammarFinalCtsByTurn) try { cts.Cancel(); cts.Dispose(); } catch { }
                    state.GrammarFinalCtsByTurn.Clear();
                    foreach (var (_, cts) in state.TranscriptThrottleCtsByTurn) try { cts.Cancel(); cts.Dispose(); } catch { }
                    state.TranscriptThrottleCtsByTurn.Clear();
                    foreach (var (_, cts) in state.TranslationThrottleCtsByTurn) try { cts.Cancel(); cts.Dispose(); } catch { }
                    state.TranslationThrottleCtsByTurn.Clear();
                }
                state.TranscriptSemaphoresByTurn.Clear();
                state.TranslationSemaphoresByTurn.Clear();
            }
        }
    }

    private void RecordActivity(string conversationId)
    {
        _lastActivityByConversation[conversationId] = _timeProvider.GetUtcNow();
    }

    private void AssertSessionOwner(string userId, string conversationId)
    {
        if (!_sessionOwners.TryGetValue(conversationId, out var owner) || !string.Equals(owner, userId, StringComparison.Ordinal))
        {
            throw new NotFoundException("Không tìm thấy hội thoại.");
        }
    }

    private string GetGroupTarget(string conversationId)
    {
        if (_sessionOwners.TryGetValue(conversationId, out var owner) && !string.IsNullOrWhiteSpace(owner))
        {
            return $"{owner}:{conversationId}";
        }
        return conversationId;
    }

    public async Task CleanupIdleSessionsAsync()
    {
        if (Interlocked.CompareExchange(ref _isSweeping, 1, 0) != 0)
        {
            return;
        }

        try
        {
            var threshold = TimeSpan.FromMinutes(_idleTimeoutMinutes);

            foreach (var (convId, _) in _lastActivityByConversation.ToArray())
            {
                try
                {
                    var now = _timeProvider.GetUtcNow();
                    if (_lastActivityByConversation.TryGetValue(convId, out var lastActivity))
                    {
                        if (now - lastActivity >= threshold)
                        {
                            await StopAllForConversationInternalAsync(convId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning("Lỗi khi dọn hội thoại không hoạt động {ConversationId}: {ExceptionType}", convId, ex.GetType().Name);
                }
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isSweeping, 0);
        }
    }

    internal void RegisterActiveSessionForTesting(
        string userId,
        string conversationId,
        string apiKey,
        string? connectionId = null,
        LiveHubType hubType = LiveHubType.Tutor)
    {
        conversationId = ConversationIdValidator.NormalizeAndValidate(conversationId);
        _sessionOwners[conversationId] = userId;
        _sessionApiKeys[conversationId] = apiKey;
        _turnStates[conversationId] = new ConversationTurnState();
        RecordActivity(conversationId);

        string conn = string.IsNullOrWhiteSpace(connectionId)
            ? $"test-conn-{conversationId}-{hubType.ToString().ToLowerInvariant()}"
            : connectionId;

        _connections[conn] = new LiveConnectionInfo(userId, conversationId, hubType);
        _hubConnections[(conversationId, hubType)] = conn;
    }

    internal bool HasActiveSession(string conversationId) => _sessionOwners.ContainsKey(conversationId);
    internal bool HasApiKey(string conversationId) => _sessionApiKeys.ContainsKey(conversationId);
    internal bool HasTurnState(string conversationId) => _turnStates.ContainsKey(conversationId);
    internal bool HasBridge(string bridgeKey) => _bridges.ContainsKey(bridgeKey);
    internal bool HasConnection(string connectionId) => _connections.ContainsKey(connectionId);
    internal bool HasHubConnection(string conversationId, LiveHubType hubType) => _hubConnections.ContainsKey((conversationId, hubType));
    internal ConversationTurnState? GetTurnState(string conversationId) => _turnStates.TryGetValue(conversationId, out var s) ? s : null;

    private async Task<ConversationTurnState> GetOrInitTurnStateAsync(string conversationId, string? cefrLevel = null)
    {
        if (_turnStates.TryGetValue(conversationId, out var existing))
        {
            if (!string.IsNullOrWhiteSpace(cefrLevel))
            {
                existing.CefrLevel = cefrLevel;
            }
            return existing;
        }

        var newState = new ConversationTurnState();
        if (!string.IsNullOrWhiteSpace(cefrLevel))
        {
            newState.CefrLevel = cefrLevel;
        }

        int maxSeq = await _repo.GetMaxSequenceOrderAsync(conversationId);
        newState.LastSequenceOrder = maxSeq;

        return _turnStates.GetOrAdd(conversationId, newState);
    }

    private static (string turnId, int seq) EnsureUserTurn(ConversationTurnState state)
    {
        lock (state.SyncRoot)
        {
            if (string.IsNullOrEmpty(state.ActiveUserTurnId))
            {
                state.LastSequenceOrder += 1;
                state.ActiveUserSequence = state.LastSequenceOrder;
                state.ActiveUserTurnId = $"u_{state.ActiveUserSequence}";
                state.UserTurnHasText = false;
                state.SequenceByTurn[state.ActiveUserTurnId] = state.ActiveUserSequence;
            }
            return (state.ActiveUserTurnId, state.ActiveUserSequence);
        }
    }

    private static (string aiTurnId, int aiSeq, string? closedUserTurnId) EnsureAiTurn(ConversationTurnState state)
    {
        lock (state.SyncRoot)
        {
            string? closedUserTurnId = null;
            if (!string.IsNullOrEmpty(state.ActiveUserTurnId))
            {
                closedUserTurnId = state.ActiveUserTurnId;
                state.ActiveUserTurnId = null;
                state.UserTurnHasText = false;
            }

            state.AiIsSpeaking = true;

            if (string.IsNullOrEmpty(state.ActiveAiTurnId))
            {
                state.LastSequenceOrder += 1;
                state.ActiveAiSequence = state.LastSequenceOrder;
                state.ActiveAiTurnId = $"ai_{state.ActiveAiSequence}";
                state.SequenceByTurn[state.ActiveAiTurnId] = state.ActiveAiSequence;
            }

            return (state.ActiveAiTurnId, state.ActiveAiSequence, closedUserTurnId);
        }
    }

    private static (string? completedAiTurnId, int completedAiSeq) CompleteAiTurn(ConversationTurnState state)
    {
        lock (state.SyncRoot)
        {
            state.AiIsSpeaking = false;
            string? turnId = state.ActiveAiTurnId;
            int seq = state.ActiveAiSequence;
            state.ActiveAiTurnId = null;
            return (turnId, seq);
        }
    }

    // -------------------------------------------------------------------------
    // 1. TUTOR CONVERSATION HUB: Gemini 3.8 Live / Live Extended Thinking
    // -------------------------------------------------------------------------
    public async Task StartTutorSessionAsync(string userId, string connectionId, StartTutorSessionRequest req)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ForbiddenException("Unauthorized: user identity not found.");
        }

        string conversationId = ConversationIdValidator.NormalizeAndValidate(req.ConversationId);
        req = req with { ConversationId = conversationId };

        // One connection can only belong to one conversation
        if (_connections.TryGetValue(connectionId, out var existingConn) &&
            !string.Equals(existingConn.ConversationId, conversationId, StringComparison.Ordinal))
        {
            throw new NotFoundException("Không tìm thấy hội thoại.");
        }

        // 1. Check in-memory ownership
        if (_sessionOwners.TryGetValue(conversationId, out var existingOwner) &&
            !string.Equals(existingOwner, userId, StringComparison.Ordinal))
        {
            throw new NotFoundException("Không tìm thấy hội thoại.");
        }

        // 2. Check DB ownership and soft-delete
        var existingConv = await _repo.GetConversationByIdAsync(conversationId);
        if (existingConv is not null && (existingConv.IsDeleted || !string.Equals(existingConv.UserId, userId, StringComparison.Ordinal)))
        {
            throw new NotFoundException("Không tìm thấy hội thoại.");
        }

        bool hasBegun = false;
        try
        {
            MarkConversationStartBegin(conversationId);
            hasBegun = true;

            GeminiLiveSessionBridge? bridge = null;
            string bridgeKey = $"{conversationId}:tutor";

            try
            {
                // 3. Create or update conversation in DB
                await _repo.CreateOrUpdateConversationAsync(
                    userId: userId,
                    id: conversationId,
                    title: null,
                    req.CefrLevel,
                    req.ExtendedThinking,
                    req.VoiceName,
                    req.SpeechSpeed,
                    req.ConversationMode,
                    req.Topic,
                    req.CustomSystemPrompt);

                // 4. Handle connection reconnect / replacement for Tutor hub
                if (_hubConnections.TryGetValue((conversationId, LiveHubType.Tutor), out var oldConnId) && oldConnId != connectionId)
                {
                    _connections.TryRemove(oldConnId, out _);
                    await RemoveBridgeIfExistsAsync(bridgeKey);
                }

                // 5. Register session ownership & connection
                _sessionOwners[conversationId] = userId;
                _connections[connectionId] = new LiveConnectionInfo(userId, conversationId, LiveHubType.Tutor);
                _hubConnections[(conversationId, LiveHubType.Tutor)] = connectionId;
                RecordActivity(conversationId);

                await RemoveBridgeIfExistsAsync(bridgeKey);

                var state = await GetOrInitTurnStateAsync(conversationId, req.CefrLevel);
                state.ConversationMode = string.IsNullOrWhiteSpace(req.ConversationMode) ? "daily_conversation" : req.ConversationMode.Trim();
                state.Topic = (req.Topic ?? string.Empty).Trim();
                state.VoiceName = string.IsNullOrWhiteSpace(req.VoiceName) ? "Aoede" : req.VoiceName.Trim();
                state.SpeechSpeed = req.SpeechSpeed <= 0 ? 1.0 : req.SpeechSpeed;
                state.Accent = string.IsNullOrWhiteSpace(req.Accent) ? "US" : req.Accent.Trim();
                state.TtsModelCode = string.IsNullOrWhiteSpace(req.TtsModelCode) ? "gemini-3.8-live" : req.TtsModelCode.Trim();
                // The prompt and the history seed both use at most the last 16 turns.
                var previousMessages = (await _repo.GetRecentMessagesAsync(userId, conversationId, 16)).ToList();

                string modelId = req.ExtendedThinking
                    ? "models/gemini-3.8-live-extended-thinking"
                    : "models/gemini-3.8-live";

                string systemInstruction = await _promptBuilder.BuildSystemInstructionAsync(
                    req.CefrLevel,
                    req.ExtendedThinking,
                    req.VoiceName,
                    req.SpeechSpeed,
                    req.ConversationMode,
                    req.Topic ?? string.Empty,
                    req.CustomSystemPrompt,
                    previousMessages);

                var generationConfig = new Dictionary<string, object>
                {
                    ["responseModalities"] = new[] { "AUDIO" },
                    ["speechConfig"] = new
                    {
                        voiceConfig = new
                        {
                            prebuiltVoiceConfig = new
                            {
                                voiceName = string.IsNullOrWhiteSpace(req.VoiceName) ? "Aoede" : req.VoiceName
                            }
                        }
                    }
                };

                if (req.ExtendedThinking)
                {
                    generationConfig["thinkingConfig"] = new
                    {
                        thinkingLevel = string.IsNullOrWhiteSpace(req.ThinkingLevel) ? "medium" : req.ThinkingLevel
                    };
                }

                var setupPayload = new
                {
                    setup = new
                    {
                        model = modelId,
                        generationConfig,
                        inputAudioTranscription = new { mode = "VERBATIM" },
                        outputAudioTranscription = new { },
                        contextWindowCompression = new
                        {
                            slidingWindow = new { }
                        },
                        systemInstruction = new
                        {
                            parts = new[] { new { text = systemInstruction } }
                        }
                    }
                };

                bridge = BridgeFactory != null
                    ? BridgeFactory(conversationId, "tutor")
                    : new GeminiLiveSessionBridge(conversationId, "tutor", modelId);

                string groupTarget = $"{userId}:{conversationId}";

                bridge.OnServerMessage += async (JsonNode node) =>
                {
                    try
                    {
                        string interactionStatus = node["interactionStatus"]?.GetValue<string>()
                            ?? node["serverContent"]?["interactionStatus"]?.GetValue<string>()
                            ?? "IDLE";

                        var serverContent = node["serverContent"];
                        if (serverContent is not null)
                        {
                            // 1. User inputTranscription from Tutor model
                            string? userInTranscript = serverContent["inputTranscription"]?["text"]?.GetValue<string>();
                            if (!string.IsNullOrWhiteSpace(userInTranscript))
                            {
                                string targetUserTurnId;
                                int targetUserSeq;
                                bool isAlreadyFinal = false;
                                lock (state.SyncRoot)
                                {
                                    if (!string.IsNullOrEmpty(state.ActiveUserTurnId))
                                    {
                                        targetUserTurnId = state.ActiveUserTurnId;
                                        targetUserSeq = state.ActiveUserSequence;
                                        state.UserTurnHasText = true;
                                    }
                                    else if (state.ActiveUserSequence > 0)
                                    {
                                        targetUserSeq = state.ActiveUserSequence;
                                        targetUserTurnId = state.SequenceByTurn.FirstOrDefault(kv => kv.Value == targetUserSeq).Key
                                            ?? $"u_{targetUserSeq}";
                                    }
                                    else
                                    {
                                        var pair = EnsureUserTurn(state);
                                        targetUserTurnId = pair.turnId;
                                        targetUserSeq = pair.seq;
                                        state.UserTurnHasText = true;
                                    }

                                    if (state.FinalTranscriptTurns.Contains(targetUserTurnId))
                                    {
                                        isAlreadyFinal = true;
                                    }
                                }

                                if (!isAlreadyFinal)
                                {
                                    await MergeAndBroadcastTranscriptAsync(
                                        conversationId,
                                        state,
                                        "user",
                                        targetUserTurnId,
                                        targetUserSeq,
                                        userInTranscript,
                                        isFinal: false);
                                }
                            }

                            // 2. Interruption signal
                            if (serverContent["interrupted"]?.GetValue<bool>() == true)
                            {
                                var (interruptedTurnId, interruptedSeq) = CompleteAiTurn(state);
                                if (!string.IsNullOrEmpty(interruptedTurnId))
                                {
                                    await FinalizeTurnAsync(conversationId, state, "ai", interruptedTurnId, interruptedSeq);
                                }
                                await _notifier.SendTutorInterruptedAsync(groupTarget);
                            }

                            // 3. AI outputTranscription
                            string? aiOutTranscript = serverContent["outputTranscription"]?["text"]?.GetValue<string>();
                            if (!string.IsNullOrWhiteSpace(aiOutTranscript))
                            {
                                var (aiTurnId, aiSeq, closedUserTurnId) = EnsureAiTurn(state);
                                if (!string.IsNullOrEmpty(closedUserTurnId))
                                {
                                    int uSeq = state.SequenceByTurn.TryGetValue(closedUserTurnId, out var s) ? s : state.ActiveUserSequence;
                                    _ = Task.Run(async () =>
                                    {
                                        try
                                        {
                                            await FinalizeTurnAsync(conversationId, state, "user", closedUserTurnId, uSeq);
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger?.LogError("Lỗi khi finalize lượt user {TurnId} cho {ConversationId}: {ExceptionType}", closedUserTurnId, conversationId, ex.GetType().Name);
                                        }
                                    });
                                }

                                await MergeAndBroadcastTranscriptAsync(
                                    conversationId,
                                    state,
                                    "ai",
                                    aiTurnId,
                                    aiSeq,
                                    aiOutTranscript,
                                    isFinal: false);
                            }

                            // 4. AI 24kHz audio output -> send to FE AND resample 16kHz to TranslateLiveHub
                            var parts = serverContent["modelTurn"]?["parts"]?.AsArray();
                            if (parts is not null)
                            {
                                var (aiTurnId, _, closedUserTurnId) = EnsureAiTurn(state);
                                if (!string.IsNullOrEmpty(closedUserTurnId))
                                {
                                    int uSeq = state.SequenceByTurn.TryGetValue(closedUserTurnId, out var s) ? s : state.ActiveUserSequence;
                                    _ = Task.Run(async () =>
                                    {
                                        try
                                        {
                                            await FinalizeTurnAsync(conversationId, state, "user", closedUserTurnId, uSeq);
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger?.LogError("Lỗi khi finalize lượt user {TurnId} cho {ConversationId}: {ExceptionType}", closedUserTurnId, conversationId, ex.GetType().Name);
                                        }
                                    });
                                }

                                foreach (var part in parts)
                                {
                                    string? base64Audio24k = part?["inlineData"]?["data"]?.GetValue<string>();
                                    if (!string.IsNullOrEmpty(base64Audio24k))
                                    {
                                        await _notifier.SendTutorAudioAsync(groupTarget, aiTurnId, base64Audio24k, interactionStatus);

                                        // Also stream resampled 16kHz AI audio to TranslateLiveHub
                                        string base64Audio16k = PcmAudioResampler.Resample24kTo16kBase64(base64Audio24k);
                                        if (!string.IsNullOrEmpty(base64Audio16k) &&
                                            _bridges.TryGetValue($"{conversationId}:translate", out var translateBridge) &&
                                            translateBridge.IsOpen)
                                        {
                                            try
                                            {
                                                await translateBridge.SendRealtimeAudioAsync(base64Audio16k);
                                            }
                                            catch (Exception sendEx)
                                            {
                                                _logger?.LogWarning("Lỗi khi gửi realtime audio sang translate bridge cho {ConversationId} translate: {ExceptionType}", conversationId, sendEx.GetType().Name);
                                            }
                                        }
                                    }
                                }
                            }

                            // 5. Turn completion
                            if (serverContent["turnComplete"]?.GetValue<bool>() == true)
                            {
                                var (completedAiTurnId, completedAiSeq) = CompleteAiTurn(state);
                                if (!string.IsNullOrEmpty(completedAiTurnId))
                                {
                                    await FinalizeTurnAsync(conversationId, state, "ai", completedAiTurnId, completedAiSeq);
                                }
                            }
                        }

                        await _notifier.SendInteractionStatusAsync(groupTarget, interactionStatus, modelId);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError("Lỗi khi xử lý OnServerMessage của Tutor cho hội thoại {ConversationId} tutor: {ExceptionType}", conversationId, ex.GetType().Name);
                    }
                };

                await bridge.ConnectAndSetupAsync(req.ApiKey, setupPayload);

                if (previousMessages.Count > 0)
                {
                    var historyTurns = previousMessages
                        .Where(m => !string.IsNullOrWhiteSpace(m.TranscriptLine1))
                        .TakeLast(16)
                        .Select(m => (object)new
                        {
                            role = m.Speaker == "user" ? "user" : "model",
                            parts = new[] { new { text = m.TranscriptLine1 } }
                        })
                        .ToList();

                    if (historyTurns.Count > 0)
                    {
                        await bridge.SeedConversationHistoryAsync(historyTurns);
                    }
                }

                bool isConnStillActive = _hubConnections.TryGetValue((conversationId, LiveHubType.Tutor), out var currentConn) &&
                                         string.Equals(currentConn, connectionId, StringComparison.Ordinal) &&
                                         _connections.ContainsKey(connectionId);

                if (!isConnStillActive)
                {
                    _logger?.LogWarning("Connection {ConnectionId} was disconnected during Start for {ConversationId} tutor; aborting orphaned bridge.", connectionId, conversationId);
                    await RollbackStartFailureAsync(conversationId, connectionId, LiveHubType.Tutor, bridge);
                    await CheckCleanupIfNoBridgesRemainingAsync(conversationId);
                    return;
                }

                _bridges[bridgeKey] = bridge;
                _sessionApiKeys[conversationId] = req.ApiKey;
            }
            catch (Exception ex)
            {
                _logger?.LogError("Lỗi khi kết nối hoặc cấu hình bridge cho {ConversationId} tutor: {ExceptionType}", conversationId, ex.GetType().Name);
                await RollbackStartFailureAsync(conversationId, connectionId, LiveHubType.Tutor, bridge);
                throw;
            }
        }
        finally
        {
            if (hasBegun)
            {
                MarkConversationStartEnd(conversationId);
            }
        }
    }

    // -------------------------------------------------------------------------
    // 2. TRANSCRIBE LIVE HUB: Gemini 3.5 Transcribe Live
    // -------------------------------------------------------------------------
    public async Task StartTranscribeHubSessionAsync(string userId, string connectionId, StartSubHubSessionRequest req)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ForbiddenException("Unauthorized: user identity not found.");
        }

        string conversationId = ConversationIdValidator.NormalizeAndValidate(req.ConversationId);
        req = req with { ConversationId = conversationId };

        if (_connections.TryGetValue(connectionId, out var existingConn) &&
            !string.Equals(existingConn.ConversationId, conversationId, StringComparison.Ordinal))
        {
            throw new NotFoundException("Không tìm thấy hội thoại.");
        }

        // 1. Check in-memory ownership
        if (_sessionOwners.TryGetValue(conversationId, out var existingOwner) &&
            !string.Equals(existingOwner, userId, StringComparison.Ordinal))
        {
            throw new NotFoundException("Không tìm thấy hội thoại.");
        }

        // 2. Check DB ownership and soft-delete
        var existingConv = await _repo.GetConversationByIdAsync(conversationId);
        if (existingConv is not null)
        {
            if (existingConv.IsDeleted || !string.Equals(existingConv.UserId, userId, StringComparison.Ordinal))
            {
                throw new NotFoundException("Không tìm thấy hội thoại.");
            }
        }
        else
        {
            await _repo.CreateConversationAsync(userId, conversationId, title: null, "B1", false, "Aoede", 1.0);
        }

        bool hasBegun = false;
        try
        {
            MarkConversationStartBegin(conversationId);
            hasBegun = true;

            GeminiLiveSessionBridge? bridge = null;
            string bridgeKey = $"{conversationId}:transcribe";

            try
            {
                // 3. Handle connection reconnect / replacement for Transcribe hub
                if (_hubConnections.TryGetValue((conversationId, LiveHubType.Transcribe), out var oldConnId) && oldConnId != connectionId)
                {
                    _connections.TryRemove(oldConnId, out _);
                    await RemoveBridgeIfExistsAsync(bridgeKey);
                }

                // 4. Register session ownership & connection
                _sessionOwners[conversationId] = userId;
                _connections[connectionId] = new LiveConnectionInfo(userId, conversationId, LiveHubType.Transcribe);
                _hubConnections[(conversationId, LiveHubType.Transcribe)] = connectionId;
                RecordActivity(conversationId);

                await RemoveBridgeIfExistsAsync(bridgeKey);

                var state = await GetOrInitTurnStateAsync(conversationId);

                var setupPayload = new
                {
                    setup = new
                    {
                        model = TranscribeLiveModel,
                        generationConfig = new
                        {
                            responseModalities = new[] { "TEXT" }
                        },
                        inputAudioTranscription = new
                        {
                            mode = "VERBATIM"
                        },
                        contextWindowCompression = new
                        {
                            slidingWindow = new { }
                        }
                    }
                };

                bridge = BridgeFactory != null
                    ? BridgeFactory(conversationId, "transcribe")
                    : new GeminiLiveSessionBridge(conversationId, "transcribe", TranscribeLiveModel);

                bridge.OnServerMessage += async (JsonNode node) =>
                {
                    try
                    {
                        var sc = node["serverContent"];
                        if (sc is null) return;

                        lock (state.SyncRoot)
                        {
                            if (state.AiIsSpeaking) return;
                        }

                        string? interimText = sc["interimInputTranscription"]?["text"]?.GetValue<string>();
                        if (!string.IsNullOrWhiteSpace(interimText))
                        {
                            var (userTurnId, userSeq) = EnsureUserTurn(state);
                            bool isFinal;
                            lock (state.SyncRoot)
                            {
                                isFinal = state.FinalTranscriptTurns.Contains(userTurnId);
                                if (!isFinal) state.UserTurnHasText = true;
                            }
                            if (!isFinal)
                            {
                                await BroadcastInterimTranscriptAsync(conversationId, state, "user", userTurnId, userSeq, interimText);
                            }
                        }

                        string? finalText = sc["inputTranscription"]?["text"]?.GetValue<string>();
                        if (!string.IsNullOrWhiteSpace(finalText))
                        {
                            var (userTurnId, userSeq) = EnsureUserTurn(state);
                            bool isFinal;
                            lock (state.SyncRoot)
                            {
                                isFinal = state.FinalTranscriptTurns.Contains(userTurnId);
                                if (!isFinal) state.UserTurnHasText = true;
                            }
                            if (!isFinal)
                            {
                                await MergeAndBroadcastTranscriptAsync(
                                    conversationId,
                                    state,
                                    "user",
                                    userTurnId,
                                    userSeq,
                                    finalText,
                                    isFinal: false,
                                    sourceHub: "transcribe");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError("Lỗi khi xử lý OnServerMessage của Transcribe cho hội thoại {ConversationId} transcribe: {ExceptionType}", conversationId, ex.GetType().Name);
                    }
                };

                await bridge.ConnectAndSetupAsync(req.ApiKey, setupPayload);

                bool isConnStillActive = _hubConnections.TryGetValue((conversationId, LiveHubType.Transcribe), out var currentConn) &&
                                         string.Equals(currentConn, connectionId, StringComparison.Ordinal) &&
                                         _connections.ContainsKey(connectionId);

                if (!isConnStillActive)
                {
                    _logger?.LogWarning("Connection {ConnectionId} was disconnected during Start for {ConversationId} transcribe; aborting orphaned bridge.", connectionId, conversationId);
                    await RollbackStartFailureAsync(conversationId, connectionId, LiveHubType.Transcribe, bridge);
                    await CheckCleanupIfNoBridgesRemainingAsync(conversationId);
                    return;
                }

                _bridges[bridgeKey] = bridge;
                _sessionApiKeys[conversationId] = req.ApiKey;
            }
            catch (Exception ex)
            {
                _logger?.LogError("Lỗi khi kết nối hoặc khởi tạo transcribe bridge cho {ConversationId} transcribe: {ExceptionType}", conversationId, ex.GetType().Name);
                await RollbackStartFailureAsync(conversationId, connectionId, LiveHubType.Transcribe, bridge);
                throw;
            }
        }
        finally
        {
            if (hasBegun)
            {
                MarkConversationStartEnd(conversationId);
            }
        }
    }

    // -------------------------------------------------------------------------
    // 3. TRANSLATE LIVE HUB: Gemini 3.5 Live Translate
    // -------------------------------------------------------------------------
    public async Task StartTranslateHubSessionAsync(string userId, string connectionId, StartSubHubSessionRequest req)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ForbiddenException("Unauthorized: user identity not found.");
        }

        string conversationId = ConversationIdValidator.NormalizeAndValidate(req.ConversationId);
        req = req with { ConversationId = conversationId };

        if (_connections.TryGetValue(connectionId, out var existingConn) &&
            !string.Equals(existingConn.ConversationId, conversationId, StringComparison.Ordinal))
        {
            throw new NotFoundException("Không tìm thấy hội thoại.");
        }

        // 1. Check in-memory ownership
        if (_sessionOwners.TryGetValue(conversationId, out var existingOwner) &&
            !string.Equals(existingOwner, userId, StringComparison.Ordinal))
        {
            throw new NotFoundException("Không tìm thấy hội thoại.");
        }

        // 2. Check DB ownership and soft-delete
        var existingConv = await _repo.GetConversationByIdAsync(conversationId);
        if (existingConv is not null)
        {
            if (existingConv.IsDeleted || !string.Equals(existingConv.UserId, userId, StringComparison.Ordinal))
            {
                throw new NotFoundException("Không tìm thấy hội thoại.");
            }
        }
        else
        {
            await _repo.CreateConversationAsync(userId, conversationId, title: null, "B1", false, "Aoede", 1.0);
        }

        bool hasBegun = false;
        try
        {
            MarkConversationStartBegin(conversationId);
            hasBegun = true;

            GeminiLiveSessionBridge? bridge = null;
            string bridgeKey = $"{conversationId}:translate";

            try
            {
                // 3. Handle connection reconnect / replacement for Translate hub
                if (_hubConnections.TryGetValue((conversationId, LiveHubType.Translate), out var oldConnId) && oldConnId != connectionId)
                {
                    _connections.TryRemove(oldConnId, out _);
                    await RemoveBridgeIfExistsAsync(bridgeKey);
                }

                // 4. Register session ownership & connection
                _sessionOwners[conversationId] = userId;
                _connections[connectionId] = new LiveConnectionInfo(userId, conversationId, LiveHubType.Translate);
                _hubConnections[(conversationId, LiveHubType.Translate)] = connectionId;
                RecordActivity(conversationId);

                await RemoveBridgeIfExistsAsync(bridgeKey);

                var state = await GetOrInitTurnStateAsync(conversationId);

                var setupPayload = new
                {
                    setup = new
                    {
                        model = TranslateLiveModel,
                        generationConfig = new
                        {
                            responseModalities = new[] { "AUDIO" },
                            translationConfig = new
                            {
                                targetLanguageCode = "vi",
                                echoTargetLanguage = true
                            }
                        },
                        inputAudioTranscription = new { },
                        outputAudioTranscription = new { }
                    }
                };

                bridge = BridgeFactory != null
                    ? BridgeFactory(conversationId, "translate")
                    : new GeminiLiveSessionBridge(conversationId, "translate", TranslateLiveModel);

                bridge.OnServerMessage += async (JsonNode node) =>
                {
                    try
                    {
                        var sc = node["serverContent"];
                        if (sc is null) return;

                        string? translatedViChunk = sc["outputTranscription"]?["text"]?.GetValue<string>();
                        if (!string.IsNullOrWhiteSpace(translatedViChunk))
                        {
                            string targetTurnId;
                            int seq;
                            string speaker;

                            lock (state.SyncRoot)
                            {
                                if (state.AiIsSpeaking && !string.IsNullOrEmpty(state.ActiveAiTurnId))
                                {
                                    targetTurnId = state.ActiveAiTurnId;
                                    seq = state.ActiveAiSequence;
                                    speaker = "ai";
                                }
                                else if (!string.IsNullOrEmpty(state.ActiveUserTurnId))
                                {
                                    targetTurnId = state.ActiveUserTurnId;
                                    seq = state.ActiveUserSequence;
                                    speaker = "user";
                                }
                                else
                                {
                                    targetTurnId = state.LastSequenceOrder > 0 ? $"turn_{state.LastSequenceOrder}" : "turn_1";
                                    seq = state.LastSequenceOrder > 0 ? state.LastSequenceOrder : 1;
                                    speaker = "user";
                                }
                            }

                            if (!string.IsNullOrEmpty(targetTurnId))
                            {
                                string? accumulatedPreview = null;
                                lock (state.SyncRoot)
                                {
                                    if (state.FinalTranslationTurns.Contains(targetTurnId))
                                    {
                                        return;
                                    }

                                    bool hasFullSentenceTranslation =
                                        state.TranslationByTurn.TryGetValue(targetTurnId, out var lockedVi) &&
                                        lockedVi.Length > 0;

                                    if (!hasFullSentenceTranslation)
                                    {
                                        string prev = state.AudioTranslationBufferByTurn.TryGetValue(targetTurnId, out var p) ? p : string.Empty;
                                        accumulatedPreview = SmartTextDeduplicator.MergeChunk(prev, translatedViChunk);
                                        state.AudioTranslationBufferByTurn[targetTurnId] = accumulatedPreview;
                                    }
                                }

                                if (!string.IsNullOrWhiteSpace(accumulatedPreview))
                                {
                                    await _notifier.SendTranslateTextAsync(GetGroupTarget(conversationId), speaker, targetTurnId, seq, accumulatedPreview, isFinal: false);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError("Lỗi khi xử lý OnServerMessage của Translate cho hội thoại {ConversationId} translate: {ExceptionType}", conversationId, ex.GetType().Name);
                    }
                };

                await bridge.ConnectAndSetupAsync(req.ApiKey, setupPayload);

                bool isConnStillActive = _hubConnections.TryGetValue((conversationId, LiveHubType.Translate), out var currentConn) &&
                                         string.Equals(currentConn, connectionId, StringComparison.Ordinal) &&
                                         _connections.ContainsKey(connectionId);

                if (!isConnStillActive)
                {
                    _logger?.LogWarning("Connection {ConnectionId} was disconnected during Start for {ConversationId} translate; aborting orphaned bridge.", connectionId, conversationId);
                    await RollbackStartFailureAsync(conversationId, connectionId, LiveHubType.Translate, bridge);
                    await CheckCleanupIfNoBridgesRemainingAsync(conversationId);
                    return;
                }

                _bridges[bridgeKey] = bridge;
                _sessionApiKeys[conversationId] = req.ApiKey;
            }
            catch (Exception ex)
            {
                _logger?.LogError("Lỗi khi kết nối hoặc khởi tạo translate bridge cho {ConversationId} translate: {ExceptionType}", conversationId, ex.GetType().Name);
                await RollbackStartFailureAsync(conversationId, connectionId, LiveHubType.Translate, bridge);
                throw;
            }
        }
        finally
        {
            if (hasBegun)
            {
                MarkConversationStartEnd(conversationId);
            }
        }
    }

    // -------------------------------------------------------------------------
    // 4. GRAMMAR CORRECTION HUB: Realtime Grammar & Vocabulary Session
    // -------------------------------------------------------------------------
    public async Task StartGrammarHubSessionAsync(string userId, string connectionId, StartSubHubSessionRequest req)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ForbiddenException("Unauthorized: user identity not found.");
        }

        string conversationId = ConversationIdValidator.NormalizeAndValidate(req.ConversationId);
        req = req with { ConversationId = conversationId };

        if (_connections.TryGetValue(connectionId, out var existingConn) &&
            !string.Equals(existingConn.ConversationId, conversationId, StringComparison.Ordinal))
        {
            throw new NotFoundException("Không tìm thấy hội thoại.");
        }

        // 1. Check in-memory ownership
        if (_sessionOwners.TryGetValue(conversationId, out var existingOwner) &&
            !string.Equals(existingOwner, userId, StringComparison.Ordinal))
        {
            throw new NotFoundException("Không tìm thấy hội thoại.");
        }

        // 2. Check DB ownership and soft-delete
        var existingConv = await _repo.GetConversationByIdAsync(conversationId);
        if (existingConv is not null)
        {
            if (existingConv.IsDeleted || !string.Equals(existingConv.UserId, userId, StringComparison.Ordinal))
            {
                throw new NotFoundException("Không tìm thấy hội thoại.");
            }
        }
        else
        {
            await _repo.CreateConversationAsync(userId, conversationId, title: null, "B1", false, "Aoede", 1.0);
        }

        // 3. Handle connection reconnect / replacement for Grammar hub
        if (_hubConnections.TryGetValue((conversationId, LiveHubType.Grammar), out var oldConnId) && oldConnId != connectionId)
        {
            _connections.TryRemove(oldConnId, out _);
        }

        // 4. Register session ownership & connection
        _sessionOwners[conversationId] = userId;
        _connections[connectionId] = new LiveConnectionInfo(userId, conversationId, LiveHubType.Grammar);
        _hubConnections[(conversationId, LiveHubType.Grammar)] = connectionId;
        if (!string.IsNullOrWhiteSpace(req.ApiKey))
        {
            _sessionApiKeys[conversationId] = req.ApiKey;
        }
        RecordActivity(conversationId);
    }

    // Transcript, Per-Bubble Locked Translation & Automatic Debounced Grammar Analysis
    // -------------------------------------------------------------------------
    private async Task BroadcastInterimTranscriptAsync(
        string conversationId,
        ConversationTurnState state,
        string speaker,
        string turnId,
        int sequenceOrder,
        string interimText)
    {
        string preview;
        lock (state.SyncRoot)
        {
            if (state.FinalTranscriptTurns.Contains(turnId))
            {
                return;
            }

            string committed = state.TranscribeSourceTranscriptByTurn.TryGetValue(turnId, out var c) ? c : string.Empty;
            preview = SmartTextDeduplicator.MergeChunk(committed, interimText);
            state.SequenceByTurn[turnId] = sequenceOrder;
        }

        if (string.IsNullOrWhiteSpace(preview))
            return;

        // Client realtime broadcast frequency preserved
        await _notifier.SendTranscribeTranscriptAsync(GetGroupTarget(conversationId), speaker, turnId, sequenceOrder, preview, isFinal: false);

        ScheduleTurnTranslation(conversationId, state, speaker, turnId, sequenceOrder, preview, isFinal: false);

        if (speaker == "user")
        {
            ScheduleUserGrammarCheck(conversationId, state, turnId, preview, immediate: false);
        }
    }

    private async Task MergeAndBroadcastTranscriptAsync(
        string conversationId,
        ConversationTurnState state,
        string speaker,
        string turnId,
        int sequenceOrder,
        string deltaText,
        bool isFinal,
        string sourceHub = "tutor")
    {
        string merged;
        lock (state.SyncRoot)
        {
            if (!isFinal && state.FinalTranscriptTurns.Contains(turnId))
            {
                return;
            }

            if (speaker == "user")
            {
                var sourceDict = sourceHub == "transcribe"
                    ? state.TranscribeSourceTranscriptByTurn
                    : state.TutorSourceTranscriptByTurn;

                string existingForSource = sourceDict.TryGetValue(turnId, out var srcText) ? srcText : string.Empty;
                string updatedForSource = SmartTextDeduplicator.MergeChunk(existingForSource, deltaText);
                sourceDict[turnId] = updatedForSource;

                string tutorText = state.TutorSourceTranscriptByTurn.TryGetValue(turnId, out var tText) ? tText : string.Empty;
                string transcribeText = state.TranscribeSourceTranscriptByTurn.TryGetValue(turnId, out var trText) ? trText : string.Empty;

                merged = transcribeText.Length >= tutorText.Length ? transcribeText : tutorText;
            }
            else
            {
                string existing = state.TranscriptByTurn.TryGetValue(turnId, out var c) ? c : string.Empty;
                merged = SmartTextDeduplicator.MergeChunk(existing, deltaText);
            }

            state.TranscriptByTurn[turnId] = merged;
            state.SequenceByTurn[turnId] = sequenceOrder;
        }

        if (string.IsNullOrWhiteSpace(merged))
            return;

        // 1. Broadcast realtime immediately to client (frequency preserved)
        await _notifier.SendTranscribeTranscriptAsync(GetGroupTarget(conversationId), speaker, turnId, sequenceOrder, merged, isFinal);

        // 2. Persist with 2s throttle for non-final, immediate for final
        await PersistTranscriptThrottledAsync(conversationId, state, turnId, speaker, sequenceOrder, merged, isFinal);

        ScheduleTurnTranslation(conversationId, state, speaker, turnId, sequenceOrder, merged, isFinal);

        if (speaker == "user")
        {
            ScheduleUserGrammarCheck(conversationId, state, turnId, merged, immediate: isFinal);
        }
    }

    internal async Task PersistTranscriptThrottledAsync(
        string conversationId,
        ConversationTurnState state,
        string turnId,
        string speaker,
        int sequenceOrder,
        string text,
        bool isFinal)
    {
        if (state.IsDisposed || !_sessionOwners.ContainsKey(conversationId) || !_turnStates.ContainsKey(conversationId))
        {
            return;
        }

        if (isFinal)
        {
            CancellationTokenSource? oldCts = null;
            lock (state.SyncRoot)
            {
                state.FinalTranscriptTurns.Add(turnId);
                if (state.TranscriptThrottleCtsByTurn.Remove(turnId, out var c))
                {
                    oldCts = c;
                }
                state.PendingTranscriptByTurn.Remove(turnId);
                state.LastPersistedTranscriptAtByTurn[turnId] = _timeProvider.GetUtcNow();
            }
            if (oldCts is not null)
            {
                try { oldCts.Cancel(); oldCts.Dispose(); } catch { }
            }

            if (state.IsDisposed || !_sessionOwners.ContainsKey(conversationId))
            {
                return;
            }

            var sem = state.TranscriptSemaphoresByTurn.GetOrAdd(turnId, _ => new SemaphoreSlim(1, 1));
            OnWaitingForTurnSemaphore?.Invoke(turnId, true);
            try
            {
                await sem.WaitAsync();
                try
                {
                    await _repo.UpsertMessageTranscriptAsync(conversationId, turnId, speaker, sequenceOrder, text, isFinal: true);
                }
                catch (Exception ex)
                {
                    _logger?.LogError("Lỗi khi ghi transcript final cho hội thoại {ConversationId}: {ExceptionType}", conversationId, ex.GetType().Name);
                }
                finally
                {
                    sem.Release();
                }
            }
            catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException)
            {
                // Bỏ qua lần ghi đó
            }
            return;
        }

        bool shouldPersistNow = false;
        CancellationTokenSource? ctsToSchedule = null;

        lock (state.SyncRoot)
        {
            if (state.FinalTranscriptTurns.Contains(turnId))
            {
                return;
            }

            var now = _timeProvider.GetUtcNow();
            var last = state.LastPersistedTranscriptAtByTurn.TryGetValue(turnId, out var dt) ? dt : DateTimeOffset.MinValue;
            var elapsed = now - last;

            if (elapsed >= TimeSpan.FromSeconds(2))
            {
                shouldPersistNow = true;
                state.LastPersistedTranscriptAtByTurn[turnId] = now;
                state.PendingTranscriptByTurn.Remove(turnId);
                if (state.TranscriptThrottleCtsByTurn.Remove(turnId, out var oldCts))
                {
                    try { oldCts.Cancel(); oldCts.Dispose(); } catch { }
                }
            }
            else
            {
                state.PendingTranscriptByTurn[turnId] = (speaker, sequenceOrder, text);
                if (!state.TranscriptThrottleCtsByTurn.ContainsKey(turnId))
                {
                    ctsToSchedule = new CancellationTokenSource();
                    state.TranscriptThrottleCtsByTurn[turnId] = ctsToSchedule;
                }
            }
        }

        if (shouldPersistNow)
        {
            if (!_sessionOwners.ContainsKey(conversationId) || !_turnStates.ContainsKey(conversationId))
            {
                return;
            }

            var sem = state.TranscriptSemaphoresByTurn.GetOrAdd(turnId, _ => new SemaphoreSlim(1, 1));
            OnWaitingForTurnSemaphore?.Invoke(turnId, false);
            try
            {
                await sem.WaitAsync();
                try
                {
                    lock (state.SyncRoot)
                    {
                        if (state.FinalTranscriptTurns.Contains(turnId))
                        {
                            return;
                        }
                    }

                    await _repo.UpsertMessageTranscriptAsync(conversationId, turnId, speaker, sequenceOrder, text, isFinal: false);
                }
                catch (Exception ex)
                {
                    _logger?.LogError("Lỗi khi ghi transcript throttled cho hội thoại {ConversationId}: {ExceptionType}", conversationId, ex.GetType().Name);
                }
                finally
                {
                    sem.Release();
                }
            }
            catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException)
            {
                // Bỏ qua lần ghi đó
            }
        }
        else if (ctsToSchedule is not null)
        {
            var token = ctsToSchedule.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), _timeProvider, token);
                    (string Speaker, int Seq, string Text) toPersist;
                    lock (state.SyncRoot)
                    {
                        if (token.IsCancellationRequested || state.FinalTranscriptTurns.Contains(turnId) || !state.PendingTranscriptByTurn.Remove(turnId, out toPersist))
                            return;

                        state.TranscriptThrottleCtsByTurn.Remove(turnId);
                        state.LastPersistedTranscriptAtByTurn[turnId] = _timeProvider.GetUtcNow();
                    }

                    if (!_sessionOwners.ContainsKey(conversationId) || !_turnStates.ContainsKey(conversationId))
                    {
                        return;
                    }

                    var turnSem = state.TranscriptSemaphoresByTurn.GetOrAdd(turnId, _ => new SemaphoreSlim(1, 1));
                    try
                    {
                        await turnSem.WaitAsync(token);
                        try
                        {
                            lock (state.SyncRoot)
                            {
                                if (token.IsCancellationRequested || state.FinalTranscriptTurns.Contains(turnId))
                                {
                                    return;
                                }
                            }

                            await _repo.UpsertMessageTranscriptAsync(conversationId, turnId, toPersist.Speaker, toPersist.Seq, toPersist.Text, isFinal: false);
                        }
                        finally
                        {
                            turnSem.Release();
                        }
                    }
                    catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException)
                    {
                        // Bỏ qua lần ghi đó
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger?.LogError("Lỗi khi ghi transcript delayed cho hội thoại {ConversationId}: {ExceptionType}", conversationId, ex.GetType().Name);
                }
            }, token);
        }
    }

    internal async Task PersistTranslationThrottledAsync(
        string conversationId,
        ConversationTurnState state,
        string turnId,
        string speaker,
        int sequenceOrder,
        string translatedVi,
        bool isFinal)
    {
        if (state.IsDisposed || !_sessionOwners.ContainsKey(conversationId) || !_turnStates.ContainsKey(conversationId))
        {
            return;
        }

        if (isFinal)
        {
            CancellationTokenSource? oldCts = null;
            lock (state.SyncRoot)
            {
                state.FinalTranslationTurns.Add(turnId);
                if (state.TranslationThrottleCtsByTurn.Remove(turnId, out var c))
                {
                    oldCts = c;
                }
                state.PendingTranslationByTurn.Remove(turnId);
                state.LastPersistedTranslationAtByTurn[turnId] = _timeProvider.GetUtcNow();
            }
            if (oldCts is not null)
            {
                try { oldCts.Cancel(); oldCts.Dispose(); } catch { }
            }

            if (state.IsDisposed || !_sessionOwners.ContainsKey(conversationId))
            {
                return;
            }

            var sem = state.TranslationSemaphoresByTurn.GetOrAdd(turnId, _ => new SemaphoreSlim(1, 1));
            OnWaitingForTurnSemaphore?.Invoke(turnId, true);
            try
            {
                await sem.WaitAsync();
                try
                {
                    await _repo.UpsertMessageTranslationAsync(conversationId, turnId, speaker, sequenceOrder, translatedVi);
                }
                catch (Exception ex)
                {
                    _logger?.LogError("Lỗi khi ghi translation final cho hội thoại {ConversationId}: {ExceptionType}", conversationId, ex.GetType().Name);
                }
                finally
                {
                    sem.Release();
                }
            }
            catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException)
            {
                // Bỏ qua lần ghi đó
            }
            return;
        }

        bool shouldPersistNow = false;
        CancellationTokenSource? ctsToSchedule = null;

        lock (state.SyncRoot)
        {
            if (state.FinalTranslationTurns.Contains(turnId))
            {
                return;
            }

            var now = _timeProvider.GetUtcNow();
            var last = state.LastPersistedTranslationAtByTurn.TryGetValue(turnId, out var dt) ? dt : DateTimeOffset.MinValue;
            var elapsed = now - last;

            if (elapsed >= TimeSpan.FromSeconds(2))
            {
                shouldPersistNow = true;
                state.LastPersistedTranslationAtByTurn[turnId] = now;
                state.PendingTranslationByTurn.Remove(turnId);
                if (state.TranslationThrottleCtsByTurn.Remove(turnId, out var oldCts))
                {
                    try { oldCts.Cancel(); oldCts.Dispose(); } catch { }
                }
            }
            else
            {
                state.PendingTranslationByTurn[turnId] = (speaker, sequenceOrder, translatedVi);
                if (!state.TranslationThrottleCtsByTurn.ContainsKey(turnId))
                {
                    ctsToSchedule = new CancellationTokenSource();
                    state.TranslationThrottleCtsByTurn[turnId] = ctsToSchedule;
                }
            }
        }

        if (shouldPersistNow)
        {
            if (!_sessionOwners.ContainsKey(conversationId) || !_turnStates.ContainsKey(conversationId))
            {
                return;
            }

            var sem = state.TranslationSemaphoresByTurn.GetOrAdd(turnId, _ => new SemaphoreSlim(1, 1));
            OnWaitingForTurnSemaphore?.Invoke(turnId, false);
            try
            {
                await sem.WaitAsync();
                try
                {
                    lock (state.SyncRoot)
                    {
                        if (state.FinalTranslationTurns.Contains(turnId))
                        {
                            return;
                        }
                    }

                    await _repo.UpsertMessageTranslationAsync(conversationId, turnId, speaker, sequenceOrder, translatedVi);
                }
                catch (Exception ex)
                {
                    _logger?.LogError("Lỗi khi ghi translation throttled cho hội thoại {ConversationId}: {ExceptionType}", conversationId, ex.GetType().Name);
                }
                finally
                {
                    sem.Release();
                }
            }
            catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException)
            {
                // Bỏ qua lần ghi đó
            }
        }
        else if (ctsToSchedule is not null)
        {
            var token = ctsToSchedule.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), _timeProvider, token);
                    (string Speaker, int Seq, string Text) toPersist;
                    lock (state.SyncRoot)
                    {
                        if (token.IsCancellationRequested || state.FinalTranslationTurns.Contains(turnId) || !state.PendingTranslationByTurn.Remove(turnId, out toPersist))
                            return;

                        state.TranslationThrottleCtsByTurn.Remove(turnId);
                        state.LastPersistedTranslationAtByTurn[turnId] = _timeProvider.GetUtcNow();
                    }

                    if (!_sessionOwners.ContainsKey(conversationId) || !_turnStates.ContainsKey(conversationId))
                    {
                        return;
                    }

                    var turnSem = state.TranslationSemaphoresByTurn.GetOrAdd(turnId, _ => new SemaphoreSlim(1, 1));
                    try
                    {
                        await turnSem.WaitAsync(token);
                        try
                        {
                            lock (state.SyncRoot)
                            {
                                if (token.IsCancellationRequested || state.FinalTranslationTurns.Contains(turnId))
                                {
                                    return;
                                }
                            }

                            await _repo.UpsertMessageTranslationAsync(conversationId, turnId, toPersist.Speaker, toPersist.Seq, toPersist.Text);
                        }
                        finally
                        {
                            turnSem.Release();
                        }
                    }
                    catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException)
                    {
                        // Bỏ qua lần ghi đó
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger?.LogError("Lỗi khi ghi translation delayed cho hội thoại {ConversationId}: {ExceptionType}", conversationId, ex.GetType().Name);
                }
            }, token);
        }
    }

    private void ScheduleTurnTranslation(
        string conversationId,
        ConversationTurnState state,
        string speaker,
        string turnId,
        int sequenceOrder,
        string latestEnglish,
        bool isFinal)
    {
        if (isFinal)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await ExecuteTurnTranslationAsync(conversationId, state, speaker, turnId, sequenceOrder, latestEnglish, isFinal: true);
                }
                catch (Exception ex)
                {
                    _logger?.LogError("Lỗi khi dịch final turn {TurnId} cho hội thoại {ConversationId}: {ExceptionType}", turnId, conversationId, ex.GetType().Name);
                }
            });
            return;
        }

        CancellationTokenSource cts;
        lock (state.SyncRoot)
        {
            if (state.FinalTranslationTurns.Contains(turnId))
            {
                return;
            }

            if (state.TranslationDebounceByTurn.TryGetValue(turnId, out var oldCts))
            {
                try { oldCts.Cancel(); oldCts.Dispose(); } catch { }
            }
            cts = new CancellationTokenSource();
            state.TranslationDebounceByTurn[turnId] = cts;
        }

        var token = cts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(400), _timeProvider, token);
                await ExecuteTurnTranslationAsync(conversationId, state, speaker, turnId, sequenceOrder, latestEnglish, isFinal: false, token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger?.LogError("Lỗi khi dịch turn {TurnId} cho hội thoại {ConversationId}: {ExceptionType}", turnId, conversationId, ex.GetType().Name);
            }
        }, token);
    }

    internal async Task ExecuteTurnTranslationAsync(
        string conversationId,
        ConversationTurnState state,
        string speaker,
        string turnId,
        int sequenceOrder,
        string latestEnglish,
        bool isFinal,
        CancellationToken ct = default)
    {
        try
        {
            if (!isFinal)
            {
                lock (state.SyncRoot)
                {
                    if (state.FinalTranslationTurns.Contains(turnId))
                    {
                        return;
                    }
                }
            }

            _sessionApiKeys.TryGetValue(conversationId, out var apiKey);
            string? translatedVi = await _translator.TranslateToVietnameseAsync(apiKey ?? "", latestEnglish, ct);

            if (!string.IsNullOrWhiteSpace(translatedVi) && !ct.IsCancellationRequested)
            {
                lock (state.SyncRoot)
                {
                    if (!isFinal && state.FinalTranslationTurns.Contains(turnId))
                    {
                        return;
                    }
                    state.TranslationByTurn[turnId] = translatedVi;
                }

                try
                {
                    await _notifier.SendTranslateTextAsync(GetGroupTarget(conversationId), speaker, turnId, sequenceOrder, translatedVi, isFinal, ct);
                }
                catch (Exception ex)
                {
                    _logger?.LogError("Lỗi khi gửi translate text cho {ConversationId}: {ExceptionType}", conversationId, ex.GetType().Name);
                }

                await PersistTranslationThrottledAsync(conversationId, state, turnId, speaker, sequenceOrder, translatedVi, isFinal);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger?.LogError("Lỗi khi dịch turn {TurnId} cho hội thoại {ConversationId}: {ExceptionType}", turnId, conversationId, ex.GetType().Name);
        }
    }

    internal static void CleanupTurnResources(ConversationTurnState state, string turnId)
    {
        lock (state.SyncRoot)
        {
            if (state.TranslationDebounceByTurn.Remove(turnId, out var trdCts))
            {
                try { trdCts.Cancel(); trdCts.Dispose(); } catch { }
            }
            if (state.GrammarDebounceByTurn.Remove(turnId, out var gCts))
            {
                try { gCts.Cancel(); gCts.Dispose(); } catch { }
            }
            if (state.TranscriptThrottleCtsByTurn.Remove(turnId, out var tctCts))
            {
                try { tctCts.Cancel(); tctCts.Dispose(); } catch { }
            }
            if (state.TranslationThrottleCtsByTurn.Remove(turnId, out var tltCts))
            {
                try { tltCts.Cancel(); tltCts.Dispose(); } catch { }
            }

            state.TranscriptByTurn.Remove(turnId);
            state.TutorSourceTranscriptByTurn.Remove(turnId);
            state.TranscribeSourceTranscriptByTurn.Remove(turnId);
            state.TranslationByTurn.Remove(turnId);
            state.AudioTranslationBufferByTurn.Remove(turnId);
            state.SequenceByTurn.Remove(turnId);
            state.LastAnalyzedTextByTurn.Remove(turnId);
            state.LastPersistedTranscriptAtByTurn.Remove(turnId);
            state.PendingTranscriptByTurn.Remove(turnId);
            state.LastPersistedTranslationAtByTurn.Remove(turnId);
            state.PendingTranslationByTurn.Remove(turnId);
        }

        state.TranscriptSemaphoresByTurn.TryRemove(turnId, out _);
        state.TranslationSemaphoresByTurn.TryRemove(turnId, out _);
    }

    private void ScheduleUserGrammarCheck(
        string conversationId,
        ConversationTurnState state,
        string turnId,
        string userTextSnapshot,
        bool immediate)
    {
        CancellationTokenSource cts;
        lock (state.SyncRoot)
        {
            if (state.GrammarDebounceByTurn.Remove(turnId, out var oldDebounceCts))
            {
                try { oldDebounceCts.Cancel(); oldDebounceCts.Dispose(); } catch { }
            }

            if (immediate)
            {
                if (state.GrammarFinalCtsByTurn.Remove(turnId, out var oldFinalCts))
                {
                    try { oldFinalCts.Cancel(); oldFinalCts.Dispose(); } catch { }
                }
                cts = new CancellationTokenSource();
                state.GrammarFinalCtsByTurn[turnId] = cts;
            }
            else
            {
                cts = new CancellationTokenSource();
                state.GrammarDebounceByTurn[turnId] = cts;
            }
        }

        var token = cts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                if (!immediate)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(850), _timeProvider, token);
                }

                string latestText;
                string cefrLevel;
                lock (state.SyncRoot)
                {
                    latestText = state.TranscriptByTurn.TryGetValue(turnId, out var c) && !string.IsNullOrWhiteSpace(c)
                        ? c
                        : userTextSnapshot;
                    cefrLevel = state.CefrLevel;

                    if (string.IsNullOrWhiteSpace(latestText))
                        return;

                    if (state.LastAnalyzedTextByTurn.TryGetValue(turnId, out var prevAnalyzed) &&
                        prevAnalyzed.Equals(latestText, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }

                _sessionApiKeys.TryGetValue(conversationId, out var apiKey);
                var (lastAiReply, recentContext) = BuildRecentContext(state);
                var correction = await _grammarAnalyzer.AnalyzeUserUtteranceAsync(
                    apiKey ?? "",
                    conversationId,
                    turnId,
                    latestText,
                    cefrLevel,
                    lastAiTeacherReply: lastAiReply,
                    recentTurnsContext: recentContext,
                    conversationMode: state.ConversationMode,
                    topic: state.Topic,
                    ct: token);

                if (correction is not null && !token.IsCancellationRequested)
                {
                    bool isFailed = string.Equals(correction.Status, "analysis_failed", StringComparison.OrdinalIgnoreCase);

                    if (!isFailed)
                    {
                        lock (state.SyncRoot)
                        {
                            state.LastAnalyzedTextByTurn[turnId] = latestText;
                            if (state.LastAnalyzedTextByTurn.Count > 30)
                            {
                                var oldestKey = state.LastAnalyzedTextByTurn.Keys.FirstOrDefault();
                                if (oldestKey is not null)
                                {
                                    state.LastAnalyzedTextByTurn.Remove(oldestKey);
                                }
                            }
                            state.CorrectionsById[correction.Id] = correction;
                        }

                        try
                        {
                            await _repo.SaveCorrectionAsync(MapToCorrectionEntity(correction));
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError("Lỗi khi lưu correction cho {ConversationId}: {ExceptionType}", conversationId, ex.GetType().Name);
                        }
                    }

                    try
                    {
                        await _notifier.SendLanguageCorrectionAsync(GetGroupTarget(conversationId), correction, token);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError("Lỗi khi gửi language correction cho {ConversationId}: {ExceptionType}", conversationId, ex.GetType().Name);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger?.LogError("Lỗi khi kiểm tra grammar cho turn {TurnId} trong {ConversationId}: {ExceptionType}", turnId, conversationId, ex.GetType().Name);
            }
            finally
            {
                if (immediate)
                {
                    lock (state.SyncRoot)
                    {
                        if (state.GrammarFinalCtsByTurn.TryGetValue(turnId, out var regFinalCts) && ReferenceEquals(regFinalCts, cts))
                        {
                            state.GrammarFinalCtsByTurn.Remove(turnId);
                        }
                    }
                }
                else
                {
                    lock (state.SyncRoot)
                    {
                        if (state.GrammarDebounceByTurn.TryGetValue(turnId, out var regCts) && ReferenceEquals(regCts, cts))
                        {
                            state.GrammarDebounceByTurn.Remove(turnId);
                        }
                    }
                }
                try { cts.Dispose(); } catch { }
                OnGrammarCheckCompleted?.Invoke();
            }
        }, token);
    }

    public async Task AnalyzeTurnGrammarManualAsync(
        string userId,
        string connectionId,
        string conversationId,
        string turnId,
        string transcriptText,
        string cefrLevel,
        CancellationToken ct = default)
    {
        conversationId = ConversationIdValidator.NormalizeAndValidate(conversationId);
        AssertSessionOwner(userId, conversationId);

        bool isGrammarConn = _hubConnections.TryGetValue((conversationId, LiveHubType.Grammar), out var grammarConnId) &&
            string.Equals(grammarConnId, connectionId, StringComparison.Ordinal);
        bool isTutorConn = _hubConnections.TryGetValue((conversationId, LiveHubType.Tutor), out var tutorConnId) &&
            string.Equals(tutorConnId, connectionId, StringComparison.Ordinal);

        if (!isGrammarConn && !isTutorConn)
        {
            throw new NotFoundException("Không tìm thấy hội thoại.");
        }

        RecordActivity(conversationId);

        var state = await GetOrInitTurnStateAsync(conversationId, cefrLevel);
        lock (state.SyncRoot)
        {
            bool isBeingAnalyzed = state.GrammarDebounceByTurn.ContainsKey(turnId) ||
                state.GrammarFinalCtsByTurn.ContainsKey(turnId) ||
                state.ManualGrammarInFlightTurns.Contains(turnId);

            if (isBeingAnalyzed)
            {
                return;
            }

            if (state.LastAnalyzedTextByTurn.TryGetValue(turnId, out var prevAnalyzed) &&
                prevAnalyzed.Equals(transcriptText, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            state.ManualGrammarInFlightTurns.Add(turnId);
        }

        _sessionApiKeys.TryGetValue(conversationId, out var apiKey);
        try
        {
            if (ct.IsCancellationRequested)
            {
                return;
            }

            var (lastAiReply, recentContext) = BuildRecentContext(state);
            var correction = await _grammarAnalyzer.AnalyzeUserUtteranceAsync(
                apiKey ?? "",
                conversationId,
                turnId,
                transcriptText,
                cefrLevel,
                lastAiTeacherReply: lastAiReply,
                recentTurnsContext: recentContext,
                conversationMode: state.ConversationMode,
                topic: state.Topic,
                ct: ct);

            if (correction is not null && !ct.IsCancellationRequested)
            {
                bool isFailed = string.Equals(correction.Status, "analysis_failed", StringComparison.OrdinalIgnoreCase);

                if (!isFailed)
                {
                    lock (state.SyncRoot)
                    {
                        state.LastAnalyzedTextByTurn[turnId] = transcriptText;
                        if (state.LastAnalyzedTextByTurn.Count > 30)
                        {
                            var oldestKey = state.LastAnalyzedTextByTurn.Keys.FirstOrDefault();
                            if (oldestKey is not null)
                            {
                                state.LastAnalyzedTextByTurn.Remove(oldestKey);
                            }
                        }
                        state.CorrectionsById[correction.Id] = correction;
                    }

                    try
                    {
                        await _repo.SaveCorrectionAsync(MapToCorrectionEntity(correction), ct);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError("Lỗi khi lưu manual correction cho {ConversationId}: {ExceptionType}", conversationId, ex.GetType().Name);
                    }
                }

                try
                {
                    await _notifier.SendLanguageCorrectionAsync(GetGroupTarget(conversationId), correction, ct);
                }
                catch (Exception ex)
                {
                    _logger?.LogError("Lỗi khi gửi manual language correction cho {ConversationId}: {ExceptionType}", conversationId, ex.GetType().Name);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Canceled via CancellationToken (e.g., connection aborted)
        }
        catch (Exception ex)
        {
            _logger?.LogError("Lỗi khi phân tích manual grammar cho {ConversationId}: {ExceptionType}", conversationId, ex.GetType().Name);
        }
        finally
        {
            lock (state.SyncRoot)
            {
                state.ManualGrammarInFlightTurns.Remove(turnId);
            }
        }
    }

    internal async Task FinalizeTurnAsync(
        string conversationId,
        ConversationTurnState state,
        string speaker,
        string turnId,
        int sequenceOrder)
    {
        string fullText;
        lock (state.SyncRoot)
        {
            fullText = state.TranscriptByTurn.TryGetValue(turnId, out var c) ? c : string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(fullText))
        {
            // Immediate persist for final
            await PersistTranscriptThrottledAsync(conversationId, state, turnId, speaker, sequenceOrder, fullText, isFinal: true);

            try
            {
                await _notifier.SendTranscribeTranscriptAsync(GetGroupTarget(conversationId), speaker, turnId, sequenceOrder, fullText, isFinal: true);
            }
            catch (Exception ex)
            {
                _logger?.LogError("Lỗi khi gửi final transcript cho {ConversationId}: {ExceptionType}", conversationId, ex.GetType().Name);
            }

            // Perform and await final translation
            await ExecuteTurnTranslationAsync(conversationId, state, speaker, turnId, sequenceOrder, fullText, isFinal: true);

            lock (state.SyncRoot)
            {
                state.RecentFinalizedTurns.Add((speaker, fullText));
                while (state.RecentFinalizedTurns.Count > 12)
                {
                    state.RecentFinalizedTurns.RemoveAt(0);
                }
            }

            if (speaker == "user")
            {
                ScheduleUserGrammarCheck(conversationId, state, turnId, fullText, immediate: true);
            }
            else if (speaker == "ai")
            {
                string? recentUserText = null;
                lock (state.SyncRoot)
                {
                    recentUserText = state.TranscriptByTurn
                        .Where(kv => kv.Key.StartsWith("u_"))
                        .Select(kv => kv.Value)
                        .LastOrDefault();
                }

                try
                {
                    var teacherExtracted = _grammarAnalyzer.TryExtractCorrectionFromTeacherReply(
                        conversationId,
                        turnId,
                        recentUserText,
                        fullText,
                        state.CefrLevel);

                    if (teacherExtracted is not null)
                    {
                        state.CorrectionsById[teacherExtracted.Id] = teacherExtracted;
                        try
                        {
                            await _repo.SaveCorrectionAsync(MapToCorrectionEntity(teacherExtracted));
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError("Lỗi khi lưu teacher correction cho {ConversationId}: {ExceptionType}", conversationId, ex.GetType().Name);
                        }

                        try
                        {
                            await _notifier.SendLanguageCorrectionAsync(GetGroupTarget(conversationId), teacherExtracted);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError("Lỗi khi gửi teacher correction cho {ConversationId}: {ExceptionType}", conversationId, ex.GetType().Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError("Lỗi khi trích xuất teacher correction cho {ConversationId}: {ExceptionType}", conversationId, ex.GetType().Name);
                }
            }

            // Clean up turn resources
            CleanupTurnResources(state, turnId);
        }
    }

    // -------------------------------------------------------------------------
    // Audio Routing Helpers for the 3 Separate SignalR Hubs
    // -------------------------------------------------------------------------
    public async Task ForwardTutorHubAudioAsync(string userId, string connectionId, string conversationId, string base64Pcm16k)
    {
        conversationId = ConversationIdValidator.NormalizeAndValidate(conversationId);

        // Audio sau khi phiên kết thúc:
        // khi conversation không có phiên -> bỏ qua im lặng (log Debug)
        if (!_sessionOwners.TryGetValue(conversationId, out var owner))
        {
            _logger?.LogDebug("ForwardTutorHubAudioAsync bỏ qua vì phiên {ConversationId} không hoạt động.", conversationId);
            return;
        }

        // phiên thuộc user khác -> vẫn từ chối (throw NotFoundException)
        if (!string.Equals(owner, userId, StringComparison.Ordinal))
        {
            throw new NotFoundException("Không tìm thấy hội thoại.");
        }

        if (!_hubConnections.TryGetValue((conversationId, LiveHubType.Tutor), out var currentConnId) ||
            !string.Equals(currentConnId, connectionId, StringComparison.Ordinal))
        {
            _logger?.LogDebug("ForwardTutorHubAudioAsync bỏ qua vì connection {ConnectionId} không phải connection hiện hành của phiên {ConversationId}.", connectionId, conversationId);
            return;
        }

        RecordActivity(conversationId);

        if (_turnStates.TryGetValue(conversationId, out var state))
        {
            lock (state.SyncRoot)
            {
                if (!state.AiIsSpeaking)
                {
                    EnsureUserTurn(state);
                }
            }
        }

        if (_bridges.TryGetValue($"{conversationId}:tutor", out var tutorBridge) && tutorBridge.IsOpen)
        {
            await tutorBridge.SendRealtimeAudioAsync(base64Pcm16k);
        }
    }

    public async Task ForwardTranscribeHubAudioAsync(string userId, string connectionId, string conversationId, string base64Pcm16k)
    {
        conversationId = ConversationIdValidator.NormalizeAndValidate(conversationId);

        if (!_sessionOwners.TryGetValue(conversationId, out var owner))
        {
            _logger?.LogDebug("ForwardTranscribeHubAudioAsync bỏ qua vì phiên {ConversationId} không hoạt động.", conversationId);
            return;
        }

        if (!string.Equals(owner, userId, StringComparison.Ordinal))
        {
            throw new NotFoundException("Không tìm thấy hội thoại.");
        }

        if (!_hubConnections.TryGetValue((conversationId, LiveHubType.Transcribe), out var currentConnId) ||
            !string.Equals(currentConnId, connectionId, StringComparison.Ordinal))
        {
            _logger?.LogDebug("ForwardTranscribeHubAudioAsync bỏ qua vì connection {ConnectionId} không phải connection hiện hành của phiên {ConversationId}.", connectionId, conversationId);
            return;
        }

        RecordActivity(conversationId);

        if (_bridges.TryGetValue($"{conversationId}:transcribe", out var transcribeBridge) && transcribeBridge.IsOpen)
        {
            await transcribeBridge.SendRealtimeAudioAsync(base64Pcm16k);
        }
    }

    public async Task ForwardTranslateHubAudioAsync(string userId, string connectionId, string conversationId, string base64Pcm16k)
    {
        conversationId = ConversationIdValidator.NormalizeAndValidate(conversationId);

        if (!_sessionOwners.TryGetValue(conversationId, out var owner))
        {
            _logger?.LogDebug("ForwardTranslateHubAudioAsync bỏ qua vì phiên {ConversationId} không hoạt động.", conversationId);
            return;
        }

        if (!string.Equals(owner, userId, StringComparison.Ordinal))
        {
            throw new NotFoundException("Không tìm thấy hội thoại.");
        }

        if (!_hubConnections.TryGetValue((conversationId, LiveHubType.Translate), out var currentConnId) ||
            !string.Equals(currentConnId, connectionId, StringComparison.Ordinal))
        {
            _logger?.LogDebug("ForwardTranslateHubAudioAsync bỏ qua vì connection {ConnectionId} không phải connection hiện hành của phiên {ConversationId}.", connectionId, conversationId);
            return;
        }

        RecordActivity(conversationId);

        if (_bridges.TryGetValue($"{conversationId}:translate", out var translateBridge) && translateBridge.IsOpen)
        {
            await translateBridge.SendRealtimeAudioAsync(base64Pcm16k);
        }
    }

    public async Task EndUserSpeechAsync(string userId, string connectionId, string conversationId)
    {
        conversationId = ConversationIdValidator.NormalizeAndValidate(conversationId);
        AssertSessionOwner(userId, conversationId);

        bool isCurrentTutor = _hubConnections.TryGetValue((conversationId, LiveHubType.Tutor), out var tutorConn) &&
                              string.Equals(tutorConn, connectionId, StringComparison.Ordinal);
        bool isCurrentTranscribe = _hubConnections.TryGetValue((conversationId, LiveHubType.Transcribe), out var trConn) &&
                                   string.Equals(trConn, connectionId, StringComparison.Ordinal);

        if (!isCurrentTutor && !isCurrentTranscribe)
        {
            throw new NotFoundException("Không tìm thấy hội thoại.");
        }

        RecordActivity(conversationId);

        if (_turnStates.TryGetValue(conversationId, out var state))
        {
            string? userTurnId;
            int userSeq;
            lock (state.SyncRoot)
            {
                userTurnId = state.ActiveUserTurnId;
                userSeq = state.ActiveUserSequence;
                state.ActiveUserTurnId = null;
                state.UserTurnHasText = false;
            }

            if (!string.IsNullOrEmpty(userTurnId))
            {
                await FinalizeTurnAsync(conversationId, state, "user", userTurnId, userSeq);
            }
        }

        if (_bridges.TryGetValue($"{conversationId}:transcribe", out var trBridge) && trBridge.IsOpen)
        {
            await trBridge.SendAudioStreamEndAsync();
        }
    }

    public async Task EndTranslateSpeechAsync(string userId, string connectionId, string conversationId)
    {
        conversationId = ConversationIdValidator.NormalizeAndValidate(conversationId);
        AssertSessionOwner(userId, conversationId);

        if (!_hubConnections.TryGetValue((conversationId, LiveHubType.Translate), out var currentConnId) ||
            !string.Equals(currentConnId, connectionId, StringComparison.Ordinal))
        {
            throw new NotFoundException("Không tìm thấy hội thoại.");
        }

        RecordActivity(conversationId);

        if (_bridges.TryGetValue($"{conversationId}:translate", out var tlBridge) && tlBridge.IsOpen)
        {
            await tlBridge.SendAudioStreamEndAsync();
        }
    }

    public async Task StopAllForConversationAsync(string userId, string connectionId, string conversationId)
    {
        conversationId = ConversationIdValidator.NormalizeAndValidate(conversationId);
        AssertSessionOwner(userId, conversationId);

        if (!_hubConnections.TryGetValue((conversationId, LiveHubType.Tutor), out var currentConnId) ||
            !string.Equals(currentConnId, connectionId, StringComparison.Ordinal))
        {
            throw new NotFoundException("Không tìm thấy hội thoại.");
        }

        await StopAllForConversationInternalAsync(conversationId);
    }

    public async Task OnClientDisconnectedAsync(string connectionId)
    {
        if (!_connections.TryRemove(connectionId, out var connInfo))
            return;

        var (userId, conversationId, hubType) = connInfo;

        if (_hubConnections.TryGetValue((conversationId, hubType), out var currentConnId))
        {
            if (!string.Equals(currentConnId, connectionId, StringComparison.Ordinal))
            {
                // Old connection disconnected after a newer connection replaced it -> Do not stop bridge or session
                return;
            }
            _hubConnections.TryRemove(new KeyValuePair<(string, LiveHubType), string>((conversationId, hubType), connectionId));
        }

        if (hubType == LiveHubType.Tutor)
        {
            if (IsConversationInitializing(conversationId))
            {
                await RemoveBridgeIfExistsAsync($"{conversationId}:tutor");
                await CheckCleanupIfNoBridgesRemainingAsync(conversationId);
            }
            else
            {
                await StopAllForConversationInternalAsync(conversationId);
            }
        }
        else if (hubType == LiveHubType.Transcribe)
        {
            await RemoveBridgeIfExistsAsync($"{conversationId}:transcribe");
            await CheckCleanupIfNoBridgesRemainingAsync(conversationId);
        }
        else if (hubType == LiveHubType.Translate)
        {
            await RemoveBridgeIfExistsAsync($"{conversationId}:translate");
            await CheckCleanupIfNoBridgesRemainingAsync(conversationId);
        }
        else if (hubType == LiveHubType.Grammar)
        {
            await CheckCleanupIfNoBridgesRemainingAsync(conversationId);
        }
    }

    private async Task CheckCleanupIfNoBridgesRemainingAsync(string conversationId)
    {
        if (IsConversationInitializing(conversationId))
        {
            return;
        }

        bool hasActiveBridge = _bridges.Keys.Any(k => k.StartsWith($"{conversationId}:", StringComparison.Ordinal));
        bool hasActiveConnections = _connections.Values.Any(c => c.ConversationId == conversationId);
        if (!hasActiveBridge && !hasActiveConnections && !IsConversationInitializing(conversationId))
        {
            await CleanupConversationStateAsync(conversationId);
        }
    }

    private async Task StopAllForConversationInternalAsync(string conversationId)
    {
        var keys = _bridges.Keys.Where(k => k.StartsWith($"{conversationId}:", StringComparison.Ordinal)).ToList();
        foreach (var k in keys)
        {
            await RemoveBridgeIfExistsAsync(k);
        }

        var connsToRemove = _connections.Where(kv => kv.Value.ConversationId == conversationId).Select(kv => kv.Key).ToList();
        foreach (var cid in connsToRemove)
        {
            _connections.TryRemove(cid, out _);
        }

        foreach (var hubType in new[] { LiveHubType.Tutor, LiveHubType.Transcribe, LiveHubType.Translate, LiveHubType.Grammar })
        {
            _hubConnections.TryRemove((conversationId, hubType), out _);
        }

        await CleanupConversationStateAsync(conversationId);
    }

    private async Task CleanupConversationStateAsync(string conversationId)
    {
        // 1. Remove _sessionOwners FIRST so any incoming request is immediately rejected
        _sessionOwners.TryRemove(conversationId, out _);

        try
        {
            await FlushPendingThrottledWritesAsync(conversationId);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("Lỗi khi flush pending throttled writes cho {ConversationId}: {ExceptionType}", conversationId, ex.GetType().Name);
        }
        finally
        {
            if (_turnStates.TryRemove(conversationId, out var state))
            {
                lock (state.SyncRoot)
                {
                    state.IsDisposed = true;

                    foreach (var (_, cts) in state.TranslationDebounceByTurn)
                    {
                        try { cts.Cancel(); cts.Dispose(); } catch { }
                    }
                    state.TranslationDebounceByTurn.Clear();

                    foreach (var (_, cts) in state.GrammarDebounceByTurn)
                    {
                        try { cts.Cancel(); cts.Dispose(); } catch { }
                    }
                    state.GrammarDebounceByTurn.Clear();

                    foreach (var (_, cts) in state.GrammarFinalCtsByTurn)
                    {
                        try { cts.Cancel(); cts.Dispose(); } catch { }
                    }
                    state.GrammarFinalCtsByTurn.Clear();

                    foreach (var (_, cts) in state.TranscriptThrottleCtsByTurn)
                    {
                        try { cts.Cancel(); cts.Dispose(); } catch { }
                    }
                    state.TranscriptThrottleCtsByTurn.Clear();

                    foreach (var (_, cts) in state.TranslationThrottleCtsByTurn)
                    {
                        try { cts.Cancel(); cts.Dispose(); } catch { }
                    }
                    state.TranslationThrottleCtsByTurn.Clear();

                    state.RecentFinalizedTurns.Clear();
                    state.LastAnalyzedTextByTurn.Clear();
                    lock (state.FeedbackAudioLock)
                    {
                        state.FeedbackAudioFlight.Clear();
                        state.FeedbackAudioLruOrder.Clear();
                    }
                }

                var transcriptSems = state.TranscriptSemaphoresByTurn.ToArray();
                state.TranscriptSemaphoresByTurn.Clear();

                var translationSems = state.TranslationSemaphoresByTurn.ToArray();
                state.TranslationSemaphoresByTurn.Clear();

                var semCleanupTasks = transcriptSems.Concat(translationSems).Select(async kv =>
                {
                    var sem = kv.Value;
                    try
                    {
                        bool entered = await sem.WaitAsync(TimeSpan.FromMilliseconds(200));
                        if (entered)
                        {
                            try
                            {
                                sem.Dispose();
                            }
                            catch { }
                        }
                    }
                    catch { }
                });

                await Task.WhenAll(semCleanupTasks);
            }

            _sessionApiKeys.TryRemove(conversationId, out _);
            _lastActivityByConversation.TryRemove(conversationId, out _);

            var connsToRemove = _connections.Where(kv => kv.Value.ConversationId == conversationId).Select(kv => kv.Key).ToList();
            foreach (var cid in connsToRemove)
            {
                _connections.TryRemove(cid, out _);
            }

            foreach (var hubType in new[] { LiveHubType.Tutor, LiveHubType.Transcribe, LiveHubType.Translate, LiveHubType.Grammar })
            {
                _hubConnections.TryRemove((conversationId, hubType), out _);
            }
        }
    }

    internal async Task FlushPendingThrottledWritesAsync(string conversationId)
    {
        if (_turnStates.TryGetValue(conversationId, out var state))
        {
            List<(string TurnId, string Speaker, int Seq, string Text)> transcriptsToFlush = new();
            List<(string TurnId, string Speaker, int Seq, string Text)> translationsToFlush = new();

            lock (state.SyncRoot)
            {
                foreach (var (_, cts) in state.TranscriptThrottleCtsByTurn)
                {
                    try { cts.Cancel(); cts.Dispose(); } catch { }
                }
                state.TranscriptThrottleCtsByTurn.Clear();

                foreach (var (turnId, p) in state.PendingTranscriptByTurn)
                {
                    if (!state.FinalTranscriptTurns.Contains(turnId))
                    {
                        transcriptsToFlush.Add((turnId, p.Speaker, p.Seq, p.Text));
                    }
                }
                state.PendingTranscriptByTurn.Clear();

                foreach (var (_, cts) in state.TranslationThrottleCtsByTurn)
                {
                    try { cts.Cancel(); cts.Dispose(); } catch { }
                }
                state.TranslationThrottleCtsByTurn.Clear();

                foreach (var (turnId, p) in state.PendingTranslationByTurn)
                {
                    if (!state.FinalTranslationTurns.Contains(turnId))
                    {
                        translationsToFlush.Add((turnId, p.Speaker, p.Seq, p.Text));
                    }
                }
                state.PendingTranslationByTurn.Clear();
            }

            foreach (var t in transcriptsToFlush)
            {
                var sem = state.TranscriptSemaphoresByTurn.GetOrAdd(t.TurnId, _ => new SemaphoreSlim(1, 1));
                try
                {
                    await sem.WaitAsync();
                    try
                    {
                        lock (state.SyncRoot)
                        {
                            if (state.FinalTranscriptTurns.Contains(t.TurnId))
                            {
                                continue;
                            }
                        }

                        await _repo.UpsertMessageTranscriptAsync(conversationId, t.TurnId, t.Speaker, t.Seq, t.Text, isFinal: false);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError("Lỗi khi flush transcript cho {ConversationId}: {ExceptionType}", conversationId, ex.GetType().Name);
                    }
                    finally
                    {
                        sem.Release();
                    }
                }
                catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException)
                {
                    // Bỏ qua lần ghi đó
                }
            }

            foreach (var t in translationsToFlush)
            {
                var sem = state.TranslationSemaphoresByTurn.GetOrAdd(t.TurnId, _ => new SemaphoreSlim(1, 1));
                try
                {
                    await sem.WaitAsync();
                    try
                    {
                        lock (state.SyncRoot)
                        {
                            if (state.FinalTranslationTurns.Contains(t.TurnId))
                            {
                                continue;
                            }
                        }

                        await _repo.UpsertMessageTranslationAsync(conversationId, t.TurnId, t.Speaker, t.Seq, t.Text);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError("Lỗi khi flush translation cho {ConversationId}: {ExceptionType}", conversationId, ex.GetType().Name);
                    }
                    finally
                    {
                        sem.Release();
                    }
                }
                catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException)
                {
                    // Bỏ qua lần ghi đó
                }
            }
        }
    }

    private static (string? lastAiReply, string recentContext) BuildRecentContext(ConversationTurnState state)
    {
        lock (state.SyncRoot)
        {
            var turns = state.RecentFinalizedTurns.TakeLast(6).ToList();

            string? lastAiReply = state.RecentFinalizedTurns
                .Where(t => t.Speaker.Equals("ai", StringComparison.OrdinalIgnoreCase) || t.Speaker.Equals("teacher", StringComparison.OrdinalIgnoreCase))
                .Select(t => t.Text)
                .LastOrDefault();

            var sb = new StringBuilder();
            foreach (var (speaker, text) in turns)
            {
                var role = speaker.Equals("ai", StringComparison.OrdinalIgnoreCase) || speaker.Equals("teacher", StringComparison.OrdinalIgnoreCase)
                    ? "Teacher"
                    : "Student";
                sb.AppendLine($"{role}: {text}");
            }

            string context = sb.ToString().TrimEnd();
            if (context.Length > 2000)
            {
                context = context[^2000..];
            }

            return (lastAiReply, context);
        }
    }

    public async Task<FeedbackAudioResultDto> RequestFeedbackAudioAsync(
        string userId,
        string connectionId,
        string conversationId,
        string correctionId,
        string variant,
        CancellationToken ct = default)
    {
        conversationId = ConversationIdValidator.NormalizeAndValidate(conversationId);

        // A foreign session answers exactly like a missing one, so a caller cannot probe whether another
        // user's conversation is live. The owner check runs before any other session state is consulted.
        if (!_sessionOwners.TryGetValue(conversationId, out var owner) ||
            !string.Equals(owner, userId, StringComparison.Ordinal) ||
            !_turnStates.TryGetValue(conversationId, out var state) ||
            !_sessionApiKeys.TryGetValue(conversationId, out var apiKey) ||
            string.IsNullOrWhiteSpace(apiKey))
        {
            throw new FeedbackAudioException("feedback_audio_session_ended");
        }

        bool isGrammarConn = _hubConnections.TryGetValue((conversationId, LiveHubType.Grammar), out var grammarConnId) &&
            string.Equals(grammarConnId, connectionId, StringComparison.Ordinal);
        bool isTutorConn = _hubConnections.TryGetValue((conversationId, LiveHubType.Tutor), out var tutorConnId) &&
            string.Equals(tutorConnId, connectionId, StringComparison.Ordinal);

        if (!isGrammarConn && !isTutorConn)
        {
            throw new NotFoundException("Không tìm thấy hội thoại.");
        }

        RecordActivity(conversationId);

        string? textToSynthesize = null;
        if (state.CorrectionsById.TryGetValue(correctionId, out var inMemCorr))
        {
            textToSynthesize = string.Equals(variant, "improved", StringComparison.OrdinalIgnoreCase)
                ? inMemCorr.ImprovedText
                : inMemCorr.CorrectedText;
        }
        else
        {
            var dbCorr = await _repo.GetCorrectionByIdAsync(conversationId, correctionId, ct);
            if (dbCorr is null)
            {
                throw new FeedbackAudioException("feedback_audio_not_found");
            }

            textToSynthesize = string.Equals(variant, "improved", StringComparison.OrdinalIgnoreCase)
                ? dbCorr.ImprovedText
                : dbCorr.CorrectedText;
        }

        if (string.IsNullOrWhiteSpace(textToSynthesize))
        {
            throw new FeedbackAudioException("feedback_audio_no_text");
        }

        textToSynthesize = textToSynthesize.Trim();
        if (textToSynthesize.Length > 500)
        {
            textToSynthesize = textToSynthesize[..500];
        }

        if (_feedbackAudioSynthesizer is null)
        {
            throw new InvalidOperationException("Dịch vụ tạo âm thanh phản hồi chưa được cấu hình.");
        }

        var cacheKey = (correctionId, variant.ToLowerInvariant());

        Lazy<Task<CachedFeedbackAudio>> lazyTask;
        lock (state.FeedbackAudioLock)
        {
            if (state.FeedbackAudioFlight.TryGetValue(cacheKey, out var existingLazy))
            {
                state.FeedbackAudioLruOrder.Remove(cacheKey);
                state.FeedbackAudioLruOrder.AddLast(cacheKey);
                lazyTask = existingLazy;
            }
            else
            {
                var textForTts = textToSynthesize;
                var voice = state.VoiceName;
                var speed = state.SpeechSpeed;
                var accent = state.Accent;
                var modelCode = state.TtsModelCode;

                lazyTask = new Lazy<Task<CachedFeedbackAudio>>(async () =>
                {
                    var rawResult = await _feedbackAudioSynthesizer.SynthesizeFeedbackAudioAsync(
                        apiKey,
                        textForTts,
                        voice,
                        speed,
                        accent,
                        modelCode,
                        ct);

                    byte[] bytes = Convert.FromBase64String(rawResult.AudioBase64);
                    return new CachedFeedbackAudio(rawResult.MimeType, bytes, rawResult.DurationMs);
                });

                state.FeedbackAudioFlight[cacheKey] = lazyTask;
                state.FeedbackAudioLruOrder.AddLast(cacheKey);

                while (state.FeedbackAudioLruOrder.Count > 30)
                {
                    var oldest = state.FeedbackAudioLruOrder.First!.Value;
                    state.FeedbackAudioLruOrder.RemoveFirst();
                    state.FeedbackAudioFlight.TryRemove(oldest, out _);
                }
            }
        }

        CachedFeedbackAudio cached;
        try
        {
            cached = await lazyTask.Value;
        }
        catch
        {
            lock (state.FeedbackAudioLock)
            {
                state.FeedbackAudioFlight.TryRemove(cacheKey, out _);
                state.FeedbackAudioLruOrder.Remove(cacheKey);
            }
            throw;
        }

        return new FeedbackAudioResultDto(cached.MimeType, Convert.ToBase64String(cached.AudioBytes), cached.DurationMs);
    }

    private static LanguageCorrectionEntity MapToCorrectionEntity(LanguageCorrectionDto dto)
    {
        string? issuesJson = dto.Issues is { Count: > 0 }
            ? System.Text.Json.JsonSerializer.Serialize(dto.Issues)
            : null;

        return new(
            id: dto.Id,
            conversationId: dto.ConversationId,
            turnId: dto.TurnId,
            originalText: dto.OriginalText,
            correctedText: dto.CorrectedText,
            errorCategory: dto.ErrorCategory,
            explanationVi: dto.ExplanationVi,
            cefrTip: dto.CefrTip,
            createdAtUtc: dto.Timestamp,
            status: dto.Status,
            improvedText: dto.ImprovedText,
            issuesJson: issuesJson);
    }

    private async Task RemoveBridgeIfExistsAsync(string key)
    {
        if (_bridges.TryRemove(key, out var existing))
        {
            await existing.DisposeAsync();
        }
    }

    public void Dispose()
    {
        _idleCheckTimer.Dispose();
    }
}
