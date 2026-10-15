using System.Text.Json.Nodes;
using EnglishLiveTutor.Infrastructure.Ai;

namespace EnglishLiveTutor.Infrastructure.Realtime;

/// <summary>
/// Implements Application's ILiveSessionService by coordinating turn state, WebSocket bridges,
/// debounced translation/grammar callbacks, and per-callback persistence without holding scoped
/// services inside a singleton.
/// </summary>
public sealed class GeminiLiveSessionOrchestrator : ILiveSessionService
{
    private readonly ILiveSessionRealtimeNotifier _notifier;
    private readonly IConversationRealtimeWriter _writer;
    private readonly ITurnTranslationService _translator;
    private readonly IGrammarCorrectionAnalyzer _grammarAnalyzer;
    private readonly ICefrPromptBuilder _promptBuilder;
    private readonly InMemoryLiveSessionStateStore _stateStore;
    private readonly ConversationTurnCoordinator _turnCoordinator;

    public GeminiLiveSessionOrchestrator(
        ILiveSessionRealtimeNotifier notifier,
        IConversationRealtimeWriter writer,
        ITurnTranslationService translator,
        IGrammarCorrectionAnalyzer grammarAnalyzer,
        ICefrPromptBuilder? promptBuilder = null,
        InMemoryLiveSessionStateStore? stateStore = null,
        ConversationTurnCoordinator? turnCoordinator = null)
    {
        _notifier = notifier;
        _writer = writer;
        _translator = translator;
        _grammarAnalyzer = grammarAnalyzer;
        _promptBuilder = promptBuilder ?? new CefrPromptBuilder();
        _stateStore = stateStore ?? new InMemoryLiveSessionStateStore();
        _turnCoordinator = turnCoordinator ?? new ConversationTurnCoordinator();
    }

    public InMemoryLiveSessionStateStore StateStore => _stateStore;

    private async Task<ConversationTurnState> GetOrInitTurnStateAsync(
        string conversationId,
        string? cefrLevel = null,
        int? knownMaxSequence = null,
        CancellationToken ct = default)
    {
        if (_stateStore.TryGetTurnState(conversationId, out var existing) && existing is not null)
        {
            if (!string.IsNullOrWhiteSpace(cefrLevel))
            {
                existing.CefrLevel = cefrLevel;
            }
            return existing;
        }

        int maxSeq = knownMaxSequence ?? await _writer.GetMaxSequenceOrderAsync(conversationId, ct);
        var created = new ConversationTurnState
        {
            LastSequenceOrder = maxSeq,
            CefrLevel = cefrLevel ?? "B1"
        };
        return _stateStore.GetOrAddTurnState(conversationId, created);
    }

    public async Task StartTutorSessionAsync(
        StartTutorSessionRequest req,
        string? connectionId = null,
        CancellationToken ct = default)
    {
        string key = $"{req.ConversationId}:tutor";
        await _stateStore.RemoveBridgeIfExistsAsync(key);
        _stateStore.SetApiKey(req.ConversationId, req.ApiKey);
        _stateStore.RegisterConnectionOwner(req.ConversationId, "tutor", connectionId);

        var sessionContext = await _writer.EnsureConversationForSessionAsync(req, ct);
        var state = await GetOrInitTurnStateAsync(req.ConversationId, req.CefrLevel, sessionContext.MaxSequenceOrder, ct);
        var previousMessages = sessionContext.RecentMessages;

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
                            var pair = _turnCoordinator.EnsureUserTurn(state);
                            targetUserTurnId = pair.TurnId;
                            targetUserSeq = pair.SequenceOrder;
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
                    var (interruptedTurnId, interruptedSeq) = _turnCoordinator.CompleteAiTurn(state);
                    if (!string.IsNullOrEmpty(interruptedTurnId))
                    {
                        await FinalizeTurnAsync(req.ConversationId, state, "ai", interruptedTurnId, interruptedSeq);
                    }
                    await _notifier.SendTutorInterruptedAsync(req.ConversationId);
                }

                string? aiOutTranscript = serverContent["outputTranscription"]?["text"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(aiOutTranscript))
                {
                    var (aiTurnId, aiSeq, closedUserTurnId) = _turnCoordinator.EnsureAiTurn(state);
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
                    var (aiTurnId, _, closedUserTurnId) = _turnCoordinator.EnsureAiTurn(state);
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
                                _stateStore.TryGetBridge($"{req.ConversationId}:translate", out var translateBridge) &&
                                translateBridge is not null &&
                                translateBridge.IsOpen)
                            {
                                _ = translateBridge.SendRealtimeAudioAsync(base64Audio16k);
                            }
                        }
                    }
                }

                if (serverContent["turnComplete"]?.GetValue<bool>() == true)
                {
                    var (completedAiTurnId, completedAiSeq) = _turnCoordinator.CompleteAiTurn(state);
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

            _stateStore.SetBridge(key, bridge);
        }
        catch
        {
            await bridge.DisposeAsync();
            if (!_stateStore.HasAnyBridgeForConversation(req.ConversationId))
            {
                await _stateStore.CleanupConversationAsync(req.ConversationId);
            }
            throw;
        }
    }

    public async Task StartTranscribeHubSessionAsync(
        StartSubHubSessionRequest req,
        string? connectionId = null,
        CancellationToken ct = default)
    {
        _stateStore.SetApiKey(req.ConversationId, req.ApiKey);
        _stateStore.RegisterConnectionOwner(req.ConversationId, "transcribe", connectionId);
        string key = $"{req.ConversationId}:transcribe";
        await _stateStore.RemoveBridgeIfExistsAsync(key);

        var state = await GetOrInitTurnStateAsync(req.ConversationId, ct: ct);

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
                var (userTurnId, userSeq) = _turnCoordinator.EnsureUserTurn(state);
                lock (state.SyncRoot) { state.UserTurnHasText = true; }
                await BroadcastInterimTranscriptAsync(req.ConversationId, state, "user", userTurnId, userSeq, interimText);
            }

            string? finalText = sc["inputTranscription"]?["text"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(finalText))
            {
                var (userTurnId, userSeq) = _turnCoordinator.EnsureUserTurn(state);
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
            _stateStore.SetBridge(key, bridge);
        }
        catch (Exception ex)
        {
            await bridge.DisposeAsync();
            Console.Error.WriteLine($"[TranscribeHub] Warning: {ex.Message}");
        }
    }

    public async Task StartTranslateHubSessionAsync(
        StartSubHubSessionRequest req,
        string? connectionId = null,
        CancellationToken ct = default)
    {
        _stateStore.SetApiKey(req.ConversationId, req.ApiKey);
        _stateStore.RegisterConnectionOwner(req.ConversationId, "translate", connectionId);
        string key = $"{req.ConversationId}:translate";
        await _stateStore.RemoveBridgeIfExistsAsync(key);

        var state = await GetOrInitTurnStateAsync(req.ConversationId, ct: ct);

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
            _stateStore.SetBridge(key, bridge);
        }
        catch (Exception ex)
        {
            await bridge.DisposeAsync();
            Console.Error.WriteLine($"[TranslateHub] Warning: {ex.Message}");
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
        lock (state.SyncRoot)
        {
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

        await _writer.UpsertMessageTranscriptAsync(conversationId, turnId, speaker, sequenceOrder, merged, isFinal);
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
        var cts = _stateStore.ResetTranslationDebounce(conversationId, turnId);
        var token = cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                if (!isFinal)
                {
                    await Task.Delay(400, token);
                }

                string? apiKey = _stateStore.TryGetApiKey(conversationId);
                string? translatedVi = await _translator.TranslateToVietnameseAsync(apiKey ?? "", latestEnglish, token);

                if (!string.IsNullOrWhiteSpace(translatedVi) && !token.IsCancellationRequested)
                {
                    lock (state.SyncRoot)
                    {
                        state.TranslationByTurn[turnId] = translatedVi;
                    }

                    await _writer.UpsertMessageTranslationAsync(conversationId, turnId, speaker, sequenceOrder, translatedVi, token);
                    await _notifier.SendTranslateTextAsync(conversationId, speaker, turnId, sequenceOrder, translatedVi, isFinal, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TranslateTurn {turnId}] Error: {ex.Message}");
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
        var cts = _stateStore.ResetGrammarDebounce(conversationId, turnId);
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

                string? apiKey = _stateStore.TryGetApiKey(conversationId);
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
                    await _writer.SaveCorrectionAsync(correction, token);
                    await _notifier.SendLanguageCorrectionAsync(conversationId, correction, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GrammarCheck {turnId}] Error: {ex.Message}");
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
        string? apiKey = _stateStore.TryGetApiKey(conversationId);
        var correction = await _grammarAnalyzer.AnalyzeUserUtteranceAsync(
            apiKey ?? "",
            conversationId,
            turnId,
            transcriptText,
            cefrLevel,
            ct);

        if (correction is not null)
        {
            await _writer.SaveCorrectionAsync(correction, ct);
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
        lock (state.SyncRoot)
        {
            fullText = state.TranscriptByTurn.TryGetValue(turnId, out var c) ? c : string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(fullText))
        {
            await _writer.UpsertMessageTranscriptAsync(conversationId, turnId, speaker, sequenceOrder, fullText, isFinal: true);
            await _notifier.SendTranscribeTranscriptAsync(conversationId, speaker, turnId, sequenceOrder, fullText, isFinal: true);

            ScheduleTurnTranslation(conversationId, state, speaker, turnId, sequenceOrder, fullText, isFinal: true);

            if (speaker == "user")
            {
                ScheduleUserGrammarCheck(conversationId, state, turnId, fullText, immediate: true);
            }
            else if (speaker == "ai")
            {
                string? recentUserText = _turnCoordinator.GetMostRecentUserText(state);
                var teacherExtracted = _grammarAnalyzer.TryExtractCorrectionFromTeacherReply(
                    conversationId,
                    turnId,
                    recentUserText,
                    fullText,
                    state.CefrLevel);

                if (teacherExtracted is not null)
                {
                    await _writer.SaveCorrectionAsync(teacherExtracted);
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
                _turnCoordinator.EnsureUserTurn(state);
            }
        }

        if (_stateStore.TryGetBridge($"{conversationId}:tutor", out var tutorBridge) &&
            tutorBridge is not null &&
            tutorBridge.IsOpen)
        {
            await tutorBridge.SendRealtimeAudioAsync(base64Pcm16k, ct);
        }
    }

    public async Task ForwardTranscribeHubAudioAsync(string conversationId, string base64Pcm16k, CancellationToken ct = default)
    {
        if (_stateStore.TryGetBridge($"{conversationId}:transcribe", out var transcribeBridge) &&
            transcribeBridge is not null &&
            transcribeBridge.IsOpen)
        {
            await transcribeBridge.SendRealtimeAudioAsync(base64Pcm16k, ct);
        }
    }

    public async Task ForwardTranslateHubAudioAsync(string conversationId, string base64Pcm16k, CancellationToken ct = default)
    {
        if (_stateStore.TryGetBridge($"{conversationId}:translate", out var translateBridge) &&
            translateBridge is not null &&
            translateBridge.IsOpen)
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

        if (_stateStore.TryGetBridge($"{conversationId}:transcribe", out var trBridge) &&
            trBridge is not null &&
            trBridge.IsOpen)
        {
            await trBridge.SendAudioStreamEndAsync(ct);
        }
    }

    public async Task EndTranslateSpeechAsync(string conversationId, CancellationToken ct = default)
    {
        if (_stateStore.TryGetBridge($"{conversationId}:translate", out var tlBridge) &&
            tlBridge is not null &&
            tlBridge.IsOpen)
        {
            await tlBridge.SendAudioStreamEndAsync(ct);
        }
    }

    public async Task StopAllForConversationAsync(string conversationId, string? connectionId = null, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(connectionId))
        {
            await HandleConnectionDisconnectedAsync(connectionId, roleTag: null, ct);
            if (_stateStore.HasActiveOwner(conversationId))
            {
                return;
            }
        }

        await _stateStore.CleanupConversationAsync(conversationId);
    }

    public async Task HandleConnectionDisconnectedAsync(string connectionId, string? roleTag = null, CancellationToken ct = default)
    {
        var (conversationId, orphanedBridgeKeys, noRemainingOwners) = _stateStore.UnregisterConnection(connectionId, roleTag);
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            return;
        }

        foreach (var bridgeKey in orphanedBridgeKeys)
        {
            await _stateStore.RemoveBridgeIfExistsAsync(bridgeKey);
        }

        if (noRemainingOwners)
        {
            await _stateStore.CleanupConversationAsync(conversationId);
        }
    }
}
