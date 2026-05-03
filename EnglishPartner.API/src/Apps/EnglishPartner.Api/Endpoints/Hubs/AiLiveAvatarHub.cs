using EnglishPartner.Modules.AI.Application.Abstractions;
using EnglishPartner.Modules.AI.Domain.ChatSessions;
using EnglishPartner.Modules.AI.Domain.ChatSessions.Repositories;
using EnglishPartner.Modules.AI.Infrastructure.Services;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Settings;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishPartner.Api.Endpoints.Hubs;

/// <summary>
/// SignalR Hub for handling Real-time Native Audio Dialog with Gemini Live API.
/// Supports continuing an existing session (with history context) and progressive summarization.
/// </summary>
public sealed class AiLiveAvatarHub : Hub
{
    private readonly LiveAudioSessionManager _sessionManager;
    private readonly IHubContext<AiLiveAvatarHub> _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AiLiveAvatarHub> _logger;

    public AiLiveAvatarHub(
        LiveAudioSessionManager sessionManager,
        IHubContext<AiLiveAvatarHub> hubContext,
        IServiceScopeFactory scopeFactory,
        ILogger<AiLiveAvatarHub> logger)
    {
        _sessionManager = sessionManager;
        _hubContext = hubContext;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        var userId = Context.UserIdentifier;
        _logger.LogInformation("Client {ConnectionId} (User={UserId}) connected to AiLiveAvatarHub.", connectionId, userId);

        var liveSession = _sessionManager.CreateSession(connectionId);

        // Use IHubContext for all callbacks — Hub instance is disposed after OnConnectedAsync returns
        var client = _hubContext.Clients.Client(connectionId);

        // Parse query parameters
        var contextQuery = Context.GetHttpContext()?.Request.Query;

        var voiceName = contextQuery != null && contextQuery.TryGetValue("voiceName", out var voiceValues)
            ? voiceValues.ToString() : null;

        var targetLanguage = contextQuery != null && contextQuery.TryGetValue("targetLanguage", out var langValues)
            ? langValues.ToString() : "English";

        var customPrompt = contextQuery != null && contextQuery.TryGetValue("systemPrompt", out var promptValues)
            ? promptValues.ToString() : null;

        // Optional: continue an existing session
        var existingSessionId = contextQuery != null && contextQuery.TryGetValue("sessionId", out var sidValues)
            && Guid.TryParse(sidValues.ToString(), out var parsedSid) ? parsedSid : (Guid?)null;

        // Build base system prompt
        var systemPrompt = string.IsNullOrWhiteSpace(customPrompt)
            ? $"You are a friendly and encouraging {targetLanguage} companion tutor. " +
              $"Talk directly and naturally in {targetLanguage}. " +
              "Keep sentences short and conversational. " +
              "If the student makes mistakes, gently correct them and explain why. " +
              $"Always speak in {targetLanguage}."
            : customPrompt;

        // Resolve voice metadata from catalog
        var voiceInfo = LiveVoiceCatalog.FindById(voiceName);

        // Resolve or create ChatSession + load history context
        Guid? chatSessionId = null;
        var historyContext = string.Empty;

        if (Guid.TryParse(userId, out var parsedUserId))
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var repo = scope.ServiceProvider.GetRequiredService<IChatSessionRepository>();
                var uow = scope.ServiceProvider.GetRequiredService<IAiUnitOfWork>();

                ChatSession? chatSession = null;

                // Continue existing session if provided
                if (existingSessionId.HasValue)
                {
                    chatSession = await repo.GetByIdWithMessagesAsync(existingSessionId.Value);
                    if (chatSession != null && chatSession.UserId == parsedUserId)
                    {
                        _logger.LogInformation("Continuing existing LiveAudio session {SessionId} ({MsgCount} messages).",
                            chatSession.Id, chatSession.Messages.Count);
                    }
                    else
                    {
                        chatSession = null; // Not found or wrong user, create new
                    }
                }

                // Create new session if not continuing
                if (chatSession is null)
                {
                    chatSession = ChatSession.StartLive(
                        parsedUserId,
                        $"Live Call – {DateTime.UtcNow:MMM dd, HH:mm}",
                        systemPrompt,
                        voiceInfo?.DisplayName ?? voiceName,
                        voiceInfo != null ? targetLanguage : null,
                        voiceInfo?.Gender);

                    repo.Add(chatSession);
                    await uow.SaveChangesAsync();
                    _logger.LogInformation("Created ChatSession {SessionId} for live call.", chatSession.Id);
                }

                chatSessionId = chatSession.Id;
                _sessionManager.SetChatSessionId(connectionId, chatSessionId.Value);

                // Build history context from summary + recent messages
                historyContext = await BuildHistoryContextAsync(repo, chatSession);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize ChatSession for live call. Transcriptions will not be persisted.");
            }
        }

        // Inject history into system prompt if available
        if (!string.IsNullOrWhiteSpace(historyContext))
        {
            systemPrompt += "\n\n--- Previous conversation context ---\n" + historyContext +
                            "\n--- End of previous context ---\n" +
                            "Continue the conversation naturally from where we left off.";
        }

        // Buffer transcriptions per turn, persist on TurnComplete
        var inputBuffer = new System.Text.StringBuilder();
        var outputBuffer = new System.Text.StringBuilder();

        liveSession.OnAudioReceived = async (pcmData) =>
        {
            try { await client.SendAsync("AudioChunkReceived", pcmData); }
            catch { /* Ignore if disconnected */ }
        };

        liveSession.OnTextReceived = async (text) =>
        {
            try { await client.SendAsync("TextChunkReceived", text); }
            catch { /* Ignore if disconnected */ }
        };

        liveSession.OnInputTranscriptionReceived = async (text) =>
        {
            inputBuffer.Append(text);
            try { await client.SendAsync("InputTranscriptionReceived", text); }
            catch { /* Ignore if disconnected */ }
        };

        liveSession.OnOutputTranscriptionReceived = async (text) =>
        {
            outputBuffer.Append(text);
            try { await client.SendAsync("OutputTranscriptionReceived", text); }
            catch { /* Ignore if disconnected */ }
        };

        liveSession.OnTurnComplete = async () =>
        {
            try { await client.SendAsync("TurnComplete"); }
            catch { /* Ignore if disconnected */ }

            // Persist buffered transcriptions + trigger summarization
            var sessionId = _sessionManager.GetChatSessionId(connectionId);
            var userText = inputBuffer.ToString().Trim();
            var aiText = outputBuffer.ToString().Trim();
            inputBuffer.Clear();
            outputBuffer.Clear();

            if (sessionId.HasValue && (userText.Length > 0 || aiText.Length > 0))
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IChatSessionRepository>();
                    var uow = scope.ServiceProvider.GetRequiredService<IAiUnitOfWork>();

                    var chatSession = await repo.GetByIdWithMessagesAsync(sessionId.Value);
                    if (chatSession != null)
                    {
                        if (userText.Length > 0) chatSession.AddUserMessage(userText);
                        if (aiText.Length > 0) chatSession.AddModelMessage(aiText);

                        await uow.SaveChangesAsync();
                        _logger.LogDebug("Persisted turn: user={UserLen}chars, ai={AiLen}chars to session {SessionId}.",
                            userText.Length, aiText.Length, sessionId);

                        // Check if summarization is needed
                        TriggerSummarizationIfNeeded(sessionId.Value, chatSession.Messages.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to persist turn transcriptions for session {SessionId}.", sessionId);
                }
            }
        };

        liveSession.OnError = async (errorMessage) =>
        {
            try { await client.SendAsync("SessionError", errorMessage); }
            catch { /* Ignore if disconnected */ }
        };

        // Fire-and-forget: Connect to Gemini in background so SignalR handshake completes immediately
        _ = Task.Run(async () =>
        {
            try
            {
                await liveSession.ConnectAsync(systemPrompt, voiceName);
                await client.SendAsync("SessionReady");
                _logger.LogInformation("Gemini session ready for {ConnectionId} (voice={Voice}, lang={Lang}).",
                    connectionId, voiceName ?? "default", targetLanguage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Gemini session for {ConnectionId}.", connectionId);
                try { await client.SendAsync("SessionError", ex.Message); }
                catch { /* Client may have disconnected */ }
            }
        });

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected.", Context.ConnectionId);
        await _sessionManager.RemoveSessionAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Called by the frontend to stream mic audio chunks (16-bit PCM at 16kHz, Base64-encoded).
    /// </summary>
    public async Task SendAudioChunk(string audioChunkBase64)
    {
        try
        {
            var session = _sessionManager.GetSession(Context.ConnectionId);
            if (session != null)
            {
                var audioChunk = Convert.FromBase64String(audioChunkBase64);
                await session.SendAudioAsync(audioChunk, Context.ConnectionAborted);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send audio chunk to Gemini.");
        }
    }

    /// <summary>
    /// Can be used by frontend to send raw text (e.g., button clicks or interruptions).
    /// </summary>
    public async Task SendTextMessage(string text)
    {
        try
        {
            var session = _sessionManager.GetSession(Context.ConnectionId);
            if (session != null)
            {
                await session.SendTextAsync(text, Context.ConnectionAborted);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send text to Gemini.");
        }
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Builds a text context from the latest summary + recent messages for injection into the system prompt.
    /// </summary>
    private static async Task<string> BuildHistoryContextAsync(IChatSessionRepository repo, ChatSession session)
    {
        if (session.Messages.Count == 0)
            return string.Empty;

        var sb = new System.Text.StringBuilder();

        var latestSummary = await repo.GetLatestSummaryAsync(session.Id);
        if (latestSummary is not null)
        {
            sb.AppendLine("[Summary of earlier conversation]");
            sb.AppendLine(latestSummary.Summary);
            sb.AppendLine();
        }

        // Take recent messages after the summary
        var summaryEndOrder = latestSummary?.EndOrder ?? 0;
        var recentMessages = session.Messages
            .Where(m => m.Order > summaryEndOrder)
            .OrderBy(m => m.Order)
            .TakeLast(10) // Keep short for live voice — context window is limited
            .ToList();

        if (recentMessages.Count > 0)
        {
            sb.AppendLine("[Recent messages]");
            foreach (var m in recentMessages)
            {
                var role = m.Role == ChatRole.User ? "Student" : "Tutor";
                sb.AppendLine($"{role}: {m.Text}");
            }
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Fire-and-forget background summarization when message threshold is reached.
    /// </summary>
    private void TriggerSummarizationIfNeeded(Guid sessionId, int totalMessages)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var repo = scope.ServiceProvider.GetRequiredService<IChatSessionRepository>();
                var uow = scope.ServiceProvider.GetRequiredService<IAiUnitOfWork>();
                var ai = scope.ServiceProvider.GetRequiredService<IGeminiService>();
                var config = scope.ServiceProvider.GetRequiredService<IOptions<GeminiAISettings>>().Value;

                var prevSummary = await repo.GetLatestSummaryAsync(sessionId);
                var prevEndOrder = prevSummary?.EndOrder ?? 0;
                var batchSize = config.MaxChatHistoryMessages;

                // Only summarize when we have a full batch past the last summary
                if (totalMessages < prevEndOrder + batchSize)
                    return;

                var sess = await repo.GetByIdWithMessagesAsync(sessionId);
                if (sess is null) return;

                var batchMessages = sess.Messages
                    .Where(m => m.Order > prevEndOrder && m.Order <= prevEndOrder + batchSize)
                    .OrderBy(m => m.Order)
                    .Select(m => new ChatTurn(m.Role == ChatRole.User ? "user" : "model", m.Text))
                    .ToList();

                if (batchMessages.Count == 0) return;

                var summaryText = await ai.SummarizeHistoryAsync(prevSummary?.Summary, batchMessages);

                sess.AddSummary(prevEndOrder + 1, prevEndOrder + batchMessages.Count, summaryText);
                await uow.SaveChangesAsync();

                _logger.LogInformation(
                    "Created live history summary for session {SessionId}: orders {Start}-{End} ({Len} chars).",
                    sessionId, prevEndOrder + 1, prevEndOrder + batchMessages.Count, summaryText.Length);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Background summarization failed for live session {SessionId}.", sessionId);
            }
        });
    }
}
