using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.Conversations.Contracts;
using EnglishLiveTutor.Application.Features.Conversations.Ports;
using EnglishLiveTutor.Application.Features.Prompts.Ports;
using EnglishLiveTutor.Domain.Conversations.Entities;
using EnglishLiveTutor.Infrastructure.Ai;
using EnglishLiveTutor.Infrastructure.Realtime.Sessions;
using EnglishLiveTutor.Infrastructure.Realtime.Transport;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Realtime;

/// <summary>
/// Orchestrates live multi-hub Gemini sessions (Tutor, Transcribe, Translate) through Application ports,
/// isolating transport bridges, in-memory turn state, and scoped persistence callbacks.
/// </summary>
public sealed class LiveSessionOrchestrator : ILiveSessionService, IAsyncDisposable
{
    private readonly ILiveSessionRealtimeNotifier _notifier;
    private readonly IConversationRealtimeWriter _writer;
    private readonly IAppReadDbContextFactory _readDbFactory;
    private readonly LiveSessionStateStore _stateStore;
    private readonly FastTextTranslatorFallback _translator;
    private readonly GrammarCorrectionAnalyzerService _grammarAnalyzer;
    private readonly ICefrPromptBuilder _promptBuilder;

    private readonly ConcurrentDictionary<string, GeminiLiveSessionBridge> _bridges = new(StringComparer.Ordinal);

    public LiveSessionOrchestrator(
        ILiveSessionRealtimeNotifier notifier,
        IConversationRealtimeWriter writer,
        IAppReadDbContextFactory readDbFactory,
        LiveSessionStateStore stateStore,
        FastTextTranslatorFallback translator,
        GrammarCorrectionAnalyzerService grammarAnalyzer,
        ICefrPromptBuilder? promptBuilder = null)
    {
        _notifier = notifier;
        _writer = writer;
        _readDbFactory = readDbFactory;
        _stateStore = stateStore;
        _translator = translator;
        _grammarAnalyzer = grammarAnalyzer;
        _promptBuilder = promptBuilder ?? new CefrPromptBuilder();
    }

    private async Task<ConversationTurnState> GetOrInitTurnStateAsync(
        string conversationId,
        string? cefrLevel = null,
        string? actorUserId = null,
        CancellationToken ct = default)
    {
        if (_stateStore.TryGetTurnState(conversationId, out var existing) && existing is not null)
        {
            return _stateStore.GetOrAddTurnState(conversationId, existing.LastSequenceOrder, cefrLevel, actorUserId);
        }

        int maxSeq = await _writer.GetMaxSequenceOrderAsync(conversationId, ct);
        return _stateStore.GetOrAddTurnState(conversationId, maxSeq, cefrLevel, actorUserId);
    }

    private static (string turnId, int seq) EnsureUserTurn(ConversationTurnState state)
    {
        lock (state.SyncRoot)
        {
            if (string.IsNullOrEmpty(state.ActiveUserTurnId))
            {
                state.LastSequenceOrder += 1;
                state.ActiveUserSequence = state.LastSequenceOrder;
                state.ActiveUserTurnId = $"u_{state.ActiveUserSequence}_{Guid.NewGuid().ToString("N")[..4]}";
                state.SequenceByTurn[state.ActiveUserTurnId] = state.ActiveUserSequence;
                state.UserTurnHasText = false;
            }
            return (state.ActiveUserTurnId, state.ActiveUserSequence);
        }
    }

    private static (string turnId, int seq, string? closedUserTurnId) EnsureAiTurn(ConversationTurnState state)
    {
        lock (state.SyncRoot)
        {
            if (state.AiIsSpeaking && !string.IsNullOrEmpty(state.ActiveAiTurnId))
            {
                return (state.ActiveAiTurnId, state.ActiveAiSequence, null);
            }

            string? closedUserTurn = null;
            if (!string.IsNullOrEmpty(state.ActiveUserTurnId) && state.UserTurnHasText)
            {
                closedUserTurn = state.ActiveUserTurnId;
                state.ActiveUserTurnId = null;
                state.UserTurnHasText = false;
            }

            state.LastSequenceOrder += 1;
            state.ActiveAiSequence = state.LastSequenceOrder;
            state.ActiveAiTurnId = $"ai_{state.ActiveAiSequence}_{Guid.NewGuid().ToString("N")[..4]}";
            state.SequenceByTurn[state.ActiveAiTurnId] = state.ActiveAiSequence;
            state.AiIsSpeaking = true;

            return (state.ActiveAiTurnId, state.ActiveAiSequence, closedUserTurn);
        }
    }

    private static (string? closedAiTurnId, int seq) CompleteAiTurn(ConversationTurnState state)
    {
        lock (state.SyncRoot)
        {
            var closedId = state.ActiveAiTurnId;
            var seq = state.ActiveAiSequence;
            state.ActiveAiTurnId = null;
            state.AiIsSpeaking = false;
            return (closedId, seq);
        }
    }

    public async Task StartTutorSessionAsync(
        StartTutorSessionRequest req,
        string? connectionId = null,
        string? actorUserId = null,
        CancellationToken ct = default)
    {
        string key = $"{req.ConversationId}:tutor";
        await RemoveBridgeIfExistsAsync(key);
        _stateStore.SetApiKey(req.ConversationId, req.ApiKey);

        await _writer.EnsureConversationSettingsAsync(
            req.ConversationId,
            title: null,
            req.CefrLevel,
            req.ExtendedThinking,
            req.VoiceName,
            req.SpeechSpeed,
            req.ConversationMode,
            req.Topic,
            req.CustomSystemPrompt,
            actorUserId,
            ct);

        var state = await GetOrInitTurnStateAsync(req.ConversationId, req.CefrLevel, actorUserId, ct);
        _stateStore.RegisterConnectionOwner(req.ConversationId, connectionId);

        List<ChatMessageEntity> previousMessages;
        await using (var readDb = await _readDbFactory.CreateReadDbContextAsync(ct))
        {
            previousMessages = await readDb.Messages
                .AsNoTracking()
                .Where(m => m.ConversationId == req.ConversationId && !m.IsDeleted && m.TranscriptLine1 != "")
                .OrderBy(m => m.SequenceOrder)
                .Take(16)
                .ToListAsync(ct);
        }

        string modelId = req.ExtendedThinking
            ? "models/gemini-3.8-live-extended-thinking"
            : "models/gemini-3.8-live";

        string systemInstruction = await _promptBuilder.BuildSystemInstructionAsync(
            req.CefrLevel,
            req.ExtendedThinking,
            req.VoiceName,
            req.SpeechSpeed,
            req.ConversationMode,
            req.Topic,
            req.CustomSystemPrompt,
            previousMessages,
            ct);

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

        var bridge = new GeminiLiveSessionBridge(req.ConversationId, "tutor");
        bridge.OnServerMessage += async (JsonNode node) =>
        {
            string interactionStatus = node["interactionStatus"]?.GetValue<string>()
                ?? node["serverContent"]?["interactionStatus"]?.GetValue<string>()
                ?? "IDLE";

            var serverContent = node["serverContent"];
            if (serverContent is not null)
            {
                string? userInTranscript = serverContent["inputTranscription"]?["text"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(userInTranscript))
                {
                    string targetUserTurnId;
                    int targetUserSeq;
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
                    }

                    await MergeAndBroadcastTranscriptAsync(
                        req.ConversationId,
                        state,
                        "user",
                        targetUserTurnId,
                        targetUserSeq,
                        userInTranscript,
                        isFinal: false);
                }

                if (serverContent["interrupted"]?.GetValue<bool>() == true)
                {
                    var (interruptedTurnId, interruptedSeq) = CompleteAiTurn(state);
                    if (!string.IsNullOrEmpty(interruptedTurnId))
                    {
                        await FinalizeTurnAsync(req.ConversationId, state, "ai", interruptedTurnId, interruptedSeq);
                    }
                    await _notifier.SendTutorInterruptedAsync(req.ConversationId);
                }

                string? aiOutTranscript = serverContent["outputTranscription"]?["text"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(aiOutTranscript))
                {
                    var (aiTurnId, aiSeq, closedUserTurnId) = EnsureAiTurn(state);
                    if (!string.IsNullOrEmpty(closedUserTurnId))
                    {
                        int uSeq = state.SequenceByTurn.TryGetValue(closedUserTurnId, out var s) ? s : state.ActiveUserSequence;
                        _ = FinalizeTurnAsync(req.ConversationId, state, "user", closedUserTurnId, uSeq);
                    }

                    await MergeAndBroadcastTranscriptAsync(
                        req.ConversationId,
                        state,
                        "ai",
                        aiTurnId,
                        aiSeq,
                        aiOutTranscript,
                        isFinal: false);
                }

                var parts = serverContent["modelTurn"]?["parts"]?.AsArray();
                if (parts is not null)
                {
                    var (aiTurnId, _, closedUserTurnId) = EnsureAiTurn(state);
                    if (!string.IsNullOrEmpty(closedUserTurnId))
                    {
                        int uSeq = state.SequenceByTurn.TryGetValue(closedUserTurnId, out var s) ? s : state.ActiveUserSequence;
                        _ = FinalizeTurnAsync(req.ConversationId, state, "user", closedUserTurnId, uSeq);
                    }

                    foreach (var part in parts)
                    {
                        string? base64Audio24k = part?["inlineData"]?["data"]?.GetValue<string>();
                        if (!string.IsNullOrEmpty(base64Audio24k))
                        {
                            await _notifier.SendTutorAudioAsync(req.ConversationId, aiTurnId, base64Audio24k, interactionStatus);

                            string base64Audio16k = PcmAudioResampler.Resample24kTo16kBase64(base64Audio24k);
                            if (!string.IsNullOrEmpty(base64Audio16k) &&
                                _bridges.TryGetValue($"{req.ConversationId}:translate", out var translateBridge) &&
                                translateBridge.IsOpen)
                            {
                                _ = translateBridge.SendRealtimeAudioAsync(base64Audio16k);
                            }
                        }
                    }
                }

                if (serverContent["turnComplete"]?.GetValue<bool>() == true)
                {
                    var (completedAiTurnId, completedAiSeq) = CompleteAiTurn(state);
                    if (!string.IsNullOrEmpty(completedAiTurnId))
                    {
                        await FinalizeTurnAsync(req.ConversationId, state, "ai", completedAiTurnId, completedAiSeq);
                    }
                }
            }

            await _notifier.SendInteractionStatusAsync(req.ConversationId, interactionStatus, modelId);
        };

        try
        {
            await bridge.ConnectAndSetupAsync(req.ApiKey, setupPayload, ct);

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
                    await bridge.SeedConversationHistoryAsync(historyTurns, ct);
                }
            }

            _bridges[key] = bridge;
        }
        catch
        {
            await bridge.DisposeAsync();
            throw;
        }
    }

    public async Task StartTranscribeHubSessionAsync(
        StartSubHubSessionRequest req,
        string? connectionId = null,
        string? actorUserId = null,
        CancellationToken ct = default)
    {
        _stateStore.SetApiKey(req.ConversationId, req.ApiKey);
        string key = $"{req.ConversationId}:transcribe";
        await RemoveBridgeIfExistsAsync(key);

        var state = await GetOrInitTurnStateAsync(req.ConversationId, actorUserId: actorUserId, ct: ct);
        _stateStore.RegisterConnectionOwner(req.ConversationId, connectionId);

        var setupPayload = new
        {
            setup = new
            {
                model = "models/gemini-3.5-transcribe-live",
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

        var bridge = new GeminiLiveSessionBridge(req.ConversationId, "transcribe");

        bridge.OnServerMessage += async (JsonNode node) =>
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
                lock (state.SyncRoot) { state.UserTurnHasText = true; }
                await BroadcastInterimTranscriptAsync(req.ConversationId, state, "user", userTurnId, userSeq, interimText);
            }

            string? finalText = sc["inputTranscription"]?["text"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(finalText))
            {
                var (userTurnId, userSeq) = EnsureUserTurn(state);
                lock (state.SyncRoot) { state.UserTurnHasText = true; }

                await MergeAndBroadcastTranscriptAsync(
                    req.ConversationId,
                    state,
                    "user",
                    userTurnId,
                    userSeq,
                    finalText,
                    isFinal: false,
                    sourceHub: "transcribe");
            }
        };

        try
        {
            await bridge.ConnectAndSetupAsync(req.ApiKey, setupPayload, ct);
            _bridges[key] = bridge;
        }
        catch
        {
            await bridge.DisposeAsync();
        }
    }

    public async Task StartTranslateHubSessionAsync(
        StartSubHubSessionRequest req,
        string? connectionId = null,
        string? actorUserId = null,
        CancellationToken ct = default)
    {
        _stateStore.SetApiKey(req.ConversationId, req.ApiKey);
        string key = $"{req.ConversationId}:translate";
        await RemoveBridgeIfExistsAsync(key);

        var state = await GetOrInitTurnStateAsync(req.ConversationId, actorUserId: actorUserId, ct: ct);
        _stateStore.RegisterConnectionOwner(req.ConversationId, connectionId);

        var setupPayload = new
        {
            setup = new
            {
                model = "models/gemini-3.5-live-translate-preview",
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

        var bridge = new GeminiLiveSessionBridge(req.ConversationId, "translate");

        bridge.OnServerMessage += async (JsonNode node) =>
        {
            var sc = node["serverContent"];
            if (sc is null) return;

            string? translatedViChunk = sc["outputTranscription"]?["text"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(translatedViChunk))
            {
                string? targetTurnId = null;
                string speaker = "user";
                int seq = 0;

                lock (state.SyncRoot)
                {
                    if (state.AiIsSpeaking && !string.IsNullOrEmpty(state.ActiveAiTurnId))
                    {
                        speaker = "ai";
                        targetTurnId = state.ActiveAiTurnId;
                        seq = state.ActiveAiSequence;
                    }
                    else if (!string.IsNullOrEmpty(state.ActiveUserTurnId) && state.UserTurnHasText)
                    {
                        speaker = "user";
                        targetTurnId = state.ActiveUserTurnId;
                        seq = state.ActiveUserSequence;
                    }
                }

                if (!string.IsNullOrEmpty(targetTurnId))
                {
                    string? accumulatedPreview = null;
                    lock (state.SyncRoot)
                    {
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
                        await _notifier.SendTranslateTextAsync(req.ConversationId, speaker, targetTurnId, seq, accumulatedPreview, isFinal: false);
                    }
                }
            }
        };

        try
        {
            await bridge.ConnectAndSetupAsync(req.ApiKey, setupPayload, ct);
            _bridges[key] = bridge;
        }
        catch
        {
            await bridge.DisposeAsync();
        }
    }

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
            string committed = state.TranscribeSourceTranscriptByTurn.TryGetValue(turnId, out var c) ? c : string.Empty;
            preview = SmartTextDeduplicator.MergeChunk(committed, interimText);
            state.SequenceByTurn[turnId] = sequenceOrder;
        }

        if (string.IsNullOrWhiteSpace(preview))
            return;

        await _notifier.SendTranscribeTranscriptAsync(conversationId, speaker, turnId, sequenceOrder, preview, isFinal: false);

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
        string? actorUserId;
        lock (state.SyncRoot)
        {
            actorUserId = state.ActorUserId;
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

        await _writer.UpsertMessageTranscriptAsync(conversationId, turnId, speaker, sequenceOrder, merged, isFinal, actorUserId);

        await _notifier.SendTranscribeTranscriptAsync(conversationId, speaker, turnId, sequenceOrder, merged, isFinal);

        ScheduleTurnTranslation(conversationId, state, speaker, turnId, sequenceOrder, merged, isFinal);

        if (speaker == "user")
        {
            ScheduleUserGrammarCheck(conversationId, state, turnId, merged, immediate: isFinal);
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
        CancellationTokenSource cts;
        string? actorUserId;
        lock (state.SyncRoot)
        {
            actorUserId = state.ActorUserId;
            if (state.TranslationDebounceByTurn.TryGetValue(turnId, out var oldCts))
            {
                oldCts.Cancel();
                oldCts.Dispose();
            }
            cts = new CancellationTokenSource();
            state.TranslationDebounceByTurn[turnId] = cts;
        }

        var token = cts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                if (!isFinal)
                {
                    await Task.Delay(400, token);
                }

                _stateStore.TryGetApiKey(conversationId, out var apiKey);
                string? translatedVi = await _translator.TranslateToVietnameseAsync(apiKey ?? "", latestEnglish, token);

                if (!string.IsNullOrWhiteSpace(translatedVi) && !token.IsCancellationRequested)
                {
                    lock (state.SyncRoot)
                    {
                        state.TranslationByTurn[turnId] = translatedVi;
                    }

                    await _writer.UpsertMessageTranslationAsync(conversationId, turnId, speaker, sequenceOrder, translatedVi, actorUserId, token);

                    await _notifier.SendTranslateTextAsync(conversationId, speaker, turnId, sequenceOrder, translatedVi, isFinal, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
            }
        }, token);
    }

    private void ScheduleUserGrammarCheck(
        string conversationId,
        ConversationTurnState state,
        string turnId,
        string userTextSnapshot,
        bool immediate)
    {
        CancellationTokenSource cts;
        string? actorUserId;
        lock (state.SyncRoot)
        {
            actorUserId = state.ActorUserId;
            if (state.GrammarDebounceByTurn.TryGetValue(turnId, out var oldCts))
            {
                oldCts.Cancel();
                oldCts.Dispose();
            }
            cts = new CancellationTokenSource();
            state.GrammarDebounceByTurn[turnId] = cts;
        }

        var token = cts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                if (!immediate)
                {
                    await Task.Delay(850, token);
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

                _stateStore.TryGetApiKey(conversationId, out var apiKey);
                var correction = await _grammarAnalyzer.AnalyzeUserUtteranceAsync(
                    apiKey ?? "",
                    conversationId,
                    turnId,
                    latestText,
                    cefrLevel,
                    token);

                if (correction is not null && !token.IsCancellationRequested)
                {
                    lock (state.SyncRoot)
                    {
                        state.LastAnalyzedTextByTurn[turnId] = latestText;
                    }
                    await _writer.SaveCorrectionAsync(MapToCorrectionEntity(correction), actorUserId, token);
                    await _notifier.SendLanguageCorrectionAsync(conversationId, correction, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
            }
        }, token);
    }

    public async Task AnalyzeTurnGrammarManualAsync(
        string conversationId,
        string turnId,
        string transcriptText,
        string cefrLevel,
        CancellationToken ct = default)
    {
        _stateStore.TryGetApiKey(conversationId, out var apiKey);
        string? actorUserId = null;
        if (_stateStore.TryGetTurnState(conversationId, out var state) && state is not null)
        {
            lock (state.SyncRoot)
            {
                actorUserId = state.ActorUserId;
            }
        }

        var correction = await _grammarAnalyzer.AnalyzeUserUtteranceAsync(
            apiKey ?? "",
            conversationId,
            turnId,
            transcriptText,
            cefrLevel,
            ct);

        if (correction is not null)
        {
            await _writer.SaveCorrectionAsync(MapToCorrectionEntity(correction), actorUserId, ct);
            await _notifier.SendLanguageCorrectionAsync(conversationId, correction, ct);
        }
    }

    public async Task FinalizeTurnAsync(
        string conversationId,
        ConversationTurnState state,
        string speaker,
        string turnId,
        int sequenceOrder)
    {
        string fullText;
        string? actorUserId;
        lock (state.SyncRoot)
        {
            actorUserId = state.ActorUserId;
            fullText = state.TranscriptByTurn.TryGetValue(turnId, out var c) ? c : string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(fullText))
        {
            await _writer.UpsertMessageTranscriptAsync(conversationId, turnId, speaker, sequenceOrder, fullText, isFinal: true, actorUserId);

            await _notifier.SendTranscribeTranscriptAsync(conversationId, speaker, turnId, sequenceOrder, fullText, isFinal: true);

            ScheduleTurnTranslation(conversationId, state, speaker, turnId, sequenceOrder, fullText, isFinal: true);

            if (speaker == "user")
            {
                ScheduleUserGrammarCheck(conversationId, state, turnId, fullText, immediate: true);
            }
            else if (speaker == "ai")
            {
                string? recentUserText;
                lock (state.SyncRoot)
                {
                    recentUserText = state.TranscriptByTurn
                        .Where(kv => kv.Key.StartsWith("u_"))
                        .Select(kv => kv.Value)
                        .LastOrDefault();
                }

                var teacherExtracted = _grammarAnalyzer.TryExtractCorrectionFromTeacherReply(
                    conversationId,
                    turnId,
                    recentUserText,
                    fullText,
                    state.CefrLevel);

                if (teacherExtracted is not null)
                {
                    await _writer.SaveCorrectionAsync(MapToCorrectionEntity(teacherExtracted), actorUserId);
                    await _notifier.SendLanguageCorrectionAsync(conversationId, teacherExtracted);
                }
            }
        }
    }

    public async Task ForwardTutorHubAudioAsync(string conversationId, string base64Pcm16k, CancellationToken ct = default)
    {
        var state = await GetOrInitTurnStateAsync(conversationId, ct: ct);
        lock (state.SyncRoot)
        {
            if (!state.AiIsSpeaking)
            {
                EnsureUserTurn(state);
            }
        }

        if (_bridges.TryGetValue($"{conversationId}:tutor", out var tutorBridge) && tutorBridge.IsOpen)
        {
            await tutorBridge.SendRealtimeAudioAsync(base64Pcm16k, ct);
        }
    }

    public async Task ForwardTranscribeHubAudioAsync(string conversationId, string base64Pcm16k, CancellationToken ct = default)
    {
        if (_bridges.TryGetValue($"{conversationId}:transcribe", out var transcribeBridge) && transcribeBridge.IsOpen)
        {
            await transcribeBridge.SendRealtimeAudioAsync(base64Pcm16k, ct);
        }
    }

    public async Task ForwardTranslateHubAudioAsync(string conversationId, string base64Pcm16k, CancellationToken ct = default)
    {
        if (_bridges.TryGetValue($"{conversationId}:translate", out var translateBridge) && translateBridge.IsOpen)
        {
            await translateBridge.SendRealtimeAudioAsync(base64Pcm16k, ct);
        }
    }

    public async Task EndUserSpeechAsync(string conversationId, CancellationToken ct = default)
    {
        var state = await GetOrInitTurnStateAsync(conversationId, ct: ct);
        string? userTurnId;
        int userSeq;
        lock (state.SyncRoot)
        {
            userTurnId = state.ActiveUserTurnId;
            userSeq = state.ActiveUserSequence;
        }

        if (!string.IsNullOrEmpty(userTurnId))
        {
            await FinalizeTurnAsync(conversationId, state, "user", userTurnId, userSeq);
        }

        if (_bridges.TryGetValue($"{conversationId}:transcribe", out var trBridge) && trBridge.IsOpen)
        {
            await trBridge.SendAudioStreamEndAsync(ct);
        }
    }

    public async Task EndTranslateSpeechAsync(string conversationId, CancellationToken ct = default)
    {
        if (_bridges.TryGetValue($"{conversationId}:translate", out var tlBridge) && tlBridge.IsOpen)
        {
            await tlBridge.SendAudioStreamEndAsync(ct);
        }
    }

    public async Task StopAllForConversationAsync(string conversationId, CancellationToken ct = default)
    {
        var keys = _bridges.Keys.Where(k => k.StartsWith($"{conversationId}:", StringComparison.Ordinal)).ToList();
        foreach (var k in keys)
        {
            await RemoveBridgeIfExistsAsync(k);
        }

        _stateStore.RemoveConversationState(conversationId);
    }

    public async Task HandleConnectionDisconnectedAsync(string connectionId, CancellationToken ct = default)
    {
        var orphanedConversations = _stateStore.UnregisterConnectionAndGetOrphanedConversations(connectionId);
        foreach (var convId in orphanedConversations)
        {
            await StopAllForConversationAsync(convId, ct);
        }
    }

    private static LanguageCorrectionEntity MapToCorrectionEntity(LanguageCorrectionDto dto) =>
        new(
            id: dto.Id,
            conversationId: dto.ConversationId,
            turnId: dto.TurnId,
            originalText: dto.OriginalText,
            correctedText: dto.CorrectedText,
            errorCategory: dto.ErrorCategory,
            explanationVi: dto.ExplanationVi,
            cefrTip: dto.CefrTip,
            createdAtUtc: dto.Timestamp);

    private async Task RemoveBridgeIfExistsAsync(string key)
    {
        if (_bridges.TryRemove(key, out var existing))
        {
            await existing.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        var allKeys = _bridges.Keys.ToList();
        foreach (var k in allKeys)
        {
            await RemoveBridgeIfExistsAsync(k);
        }
    }
}
