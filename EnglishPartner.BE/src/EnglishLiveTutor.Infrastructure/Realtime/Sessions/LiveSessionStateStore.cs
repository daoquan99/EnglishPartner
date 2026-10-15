using System.Collections.Concurrent;

namespace EnglishLiveTutor.Infrastructure.Realtime.Sessions;

public sealed class ConversationTurnState : IDisposable
{
    public object SyncRoot { get; } = new();
    public int LastSequenceOrder { get; set; }
    public string CefrLevel { get; set; } = "B1";
    public string? ActorUserId { get; set; }

    public string? ActiveUserTurnId { get; set; }
    public int ActiveUserSequence { get; set; }
    public bool UserTurnHasText { get; set; }

    public string? ActiveAiTurnId { get; set; }
    public int ActiveAiSequence { get; set; }
    public bool AiIsSpeaking { get; set; }

    public HashSet<string> ActiveConnectionIds { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> TranscriptByTurn { get; } = new();
    public Dictionary<string, string> TutorSourceTranscriptByTurn { get; } = new();
    public Dictionary<string, string> TranscribeSourceTranscriptByTurn { get; } = new();
    public Dictionary<string, string> TranslationByTurn { get; } = new();
    public Dictionary<string, string> AudioTranslationBufferByTurn { get; } = new();
    public Dictionary<string, int> SequenceByTurn { get; } = new();
    public Dictionary<string, string> LastAnalyzedTextByTurn { get; } = new();
    public Dictionary<string, CancellationTokenSource> TranslationDebounceByTurn { get; } = new();
    public Dictionary<string, CancellationTokenSource> GrammarDebounceByTurn { get; } = new();

    public void CancelAndClearDebounces()
    {
        lock (SyncRoot)
        {
            foreach (var cts in TranslationDebounceByTurn.Values)
            {
                try { cts.Cancel(); cts.Dispose(); } catch { }
            }
            TranslationDebounceByTurn.Clear();

            foreach (var cts in GrammarDebounceByTurn.Values)
            {
                try { cts.Cancel(); cts.Dispose(); } catch { }
            }
            GrammarDebounceByTurn.Clear();
        }
    }

    public void Dispose()
    {
        CancelAndClearDebounces();
    }
}

/// <summary>
/// Process-local in-memory session state store for active live conversations, API keys, and owning SignalR connections.
/// Ensures connection disconnect only stops a conversation when its last active connection owner disconnects,
/// and removes API keys and debounce timers when a conversation session stops.
/// </summary>
public sealed class LiveSessionStateStore
{
    private readonly ConcurrentDictionary<string, ConversationTurnState> _turnStates = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _sessionApiKeys = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, HashSet<string>> _conversationsByConnection = new(StringComparer.Ordinal);
    private readonly object _connectionMapLock = new();

    public bool TryGetTurnState(string conversationId, out ConversationTurnState? state) =>
        _turnStates.TryGetValue(conversationId, out state);

    public ConversationTurnState GetOrAddTurnState(string conversationId, int initialMaxSeq, string? cefrLevel = null, string? actorUserId = null)
    {
        var state = _turnStates.GetOrAdd(conversationId, _ => new ConversationTurnState
        {
            LastSequenceOrder = initialMaxSeq,
            CefrLevel = cefrLevel ?? "B1",
            ActorUserId = actorUserId
        });

        lock (state.SyncRoot)
        {
            if (!string.IsNullOrWhiteSpace(cefrLevel))
            {
                state.CefrLevel = cefrLevel;
            }
            if (!string.IsNullOrWhiteSpace(actorUserId))
            {
                state.ActorUserId = actorUserId;
            }
        }

        return state;
    }

    public void SetApiKey(string conversationId, string apiKey)
    {
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _sessionApiKeys[conversationId] = apiKey.Trim();
        }
    }

    public bool TryGetApiKey(string conversationId, out string? apiKey) =>
        _sessionApiKeys.TryGetValue(conversationId, out apiKey);

    public void RegisterConnectionOwner(string conversationId, string? connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            return;
        }

        lock (_connectionMapLock)
        {
            if (!_conversationsByConnection.TryGetValue(connectionId, out var convSet))
            {
                convSet = new HashSet<string>(StringComparer.Ordinal);
                _conversationsByConnection[connectionId] = convSet;
            }
            convSet.Add(conversationId);

            if (_turnStates.TryGetValue(conversationId, out var state))
            {
                lock (state.SyncRoot)
                {
                    state.ActiveConnectionIds.Add(connectionId);
                }
            }
        }
    }

    /// <summary>
    /// Removes the disconnected connection from all tracked conversations and returns only those conversation IDs
    /// that now have zero remaining active connections.
    /// </summary>
    public IReadOnlyList<string> UnregisterConnectionAndGetOrphanedConversations(string connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            return Array.Empty<string>();
        }

        var orphaned = new List<string>();
        lock (_connectionMapLock)
        {
            if (!_conversationsByConnection.TryRemove(connectionId, out var convIds))
            {
                return orphaned;
            }

            foreach (var convId in convIds)
            {
                if (_turnStates.TryGetValue(convId, out var state))
                {
                    lock (state.SyncRoot)
                    {
                        state.ActiveConnectionIds.Remove(connectionId);
                        if (state.ActiveConnectionIds.Count == 0)
                        {
                            orphaned.Add(convId);
                        }
                    }
                }
                else
                {
                    orphaned.Add(convId);
                }
            }
        }

        return orphaned;
    }

    public void RemoveConversationState(string conversationId)
    {
        _sessionApiKeys.TryRemove(conversationId, out _);
        if (_turnStates.TryRemove(conversationId, out var state))
        {
            state.Dispose();
            lock (_connectionMapLock)
            {
                foreach (var connId in state.ActiveConnectionIds)
                {
                    if (_conversationsByConnection.TryGetValue(connId, out var set))
                    {
                        set.Remove(conversationId);
                        if (set.Count == 0)
                        {
                            _conversationsByConnection.TryRemove(connId, out _);
                        }
                    }
                }
            }
        }
    }
}
