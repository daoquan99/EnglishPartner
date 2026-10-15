using System.Collections.Concurrent;

namespace EnglishLiveTutor.Infrastructure.Realtime;

/// <summary>
/// Manages per-conversation turn state, active WebSocket bridges, API keys, debounce tokens,
/// and SignalR connection ownership so that disconnecting one connection does not tear down
/// a session still owned by another active connection, and full session shutdown cleans up all state.
/// </summary>
public sealed class InMemoryLiveSessionStateStore
{
    private readonly ConcurrentDictionary<string, GeminiLiveSessionBridge> _bridges = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _sessionApiKeys = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ConversationTurnState> _turnStates = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Dictionary<string, CancellationTokenSource>> _translationDebounce = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Dictionary<string, CancellationTokenSource>> _grammarDebounce = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, HashSet<string>> _bridgeConnections = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _connectionToConversation = new(StringComparer.Ordinal);
    private readonly object _ownershipLock = new();

    public void RegisterConnectionOwner(string conversationId, string roleTag, string? connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId) || string.IsNullOrWhiteSpace(conversationId))
            return;

        string bridgeKey = $"{conversationId}:{roleTag}";
        lock (_ownershipLock)
        {
            if (!_bridgeConnections.TryGetValue(bridgeKey, out var set))
            {
                set = new HashSet<string>(StringComparer.Ordinal);
                _bridgeConnections[bridgeKey] = set;
            }
            set.Add(connectionId);
            _connectionToConversation[connectionId] = conversationId;
        }
    }

    public bool HasActiveOwner(string conversationId, string? connectionId = null)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
            return false;

        lock (_ownershipLock)
        {
            if (!string.IsNullOrWhiteSpace(connectionId))
            {
                return _connectionToConversation.TryGetValue(connectionId, out var cid) &&
                       string.Equals(cid, conversationId, StringComparison.Ordinal);
            }

            return _bridgeConnections.Any(kv =>
                kv.Key.StartsWith($"{conversationId}:", StringComparison.Ordinal) &&
                kv.Value.Count > 0);
        }
    }

    public (string? ConversationId, List<string> OrphanedBridgeKeys, bool ConversationHasNoRemainingOwners) UnregisterConnection(
        string connectionId,
        string? roleTag = null)
    {
        var orphanedBridgeKeys = new List<string>();
        string? conversationId = null;
        bool noRemainingOwners = false;

        if (string.IsNullOrWhiteSpace(connectionId))
        {
            return (null, orphanedBridgeKeys, false);
        }

        lock (_ownershipLock)
        {
            if (!_connectionToConversation.TryGetValue(connectionId, out conversationId))
            {
                return (null, orphanedBridgeKeys, false);
            }

            string prefix = $"{conversationId}:";
            var matchingBridgeKeys = _bridgeConnections.Keys
                .Where(k => k.StartsWith(prefix, StringComparison.Ordinal) &&
                            (string.IsNullOrWhiteSpace(roleTag) || k.Equals($"{conversationId}:{roleTag}", StringComparison.Ordinal)))
                .ToList();

            foreach (var bridgeKey in matchingBridgeKeys)
            {
                if (_bridgeConnections.TryGetValue(bridgeKey, out var owners))
                {
                    owners.Remove(connectionId);
                    if (owners.Count == 0)
                    {
                        _bridgeConnections.TryRemove(bridgeKey, out _);
                        orphanedBridgeKeys.Add(bridgeKey);
                    }
                }
            }

            bool stillOwnsAnyBridge = _bridgeConnections.Any(kv =>
                kv.Key.StartsWith(prefix, StringComparison.Ordinal) &&
                kv.Value.Contains(connectionId));

            if (!stillOwnsAnyBridge)
            {
                _connectionToConversation.TryRemove(connectionId, out _);
            }

            noRemainingOwners = !_bridgeConnections.Any(kv =>
                kv.Key.StartsWith(prefix, StringComparison.Ordinal) &&
                kv.Value.Count > 0) &&
                !_bridges.Keys.Any(k => k.StartsWith(prefix, StringComparison.Ordinal) && !orphanedBridgeKeys.Contains(k));
        }

        return (conversationId, orphanedBridgeKeys, noRemainingOwners);
    }

    public void SetApiKey(string conversationId, string apiKey)
    {
        if (!string.IsNullOrWhiteSpace(conversationId) && !string.IsNullOrWhiteSpace(apiKey))
        {
            _sessionApiKeys[conversationId] = apiKey.Trim();
        }
    }

    public string? TryGetApiKey(string conversationId) =>
        _sessionApiKeys.TryGetValue(conversationId, out var key) ? key : null;

    public bool TryGetTurnState(string conversationId, out ConversationTurnState? state) =>
        _turnStates.TryGetValue(conversationId, out state);

    public ConversationTurnState GetOrAddTurnState(string conversationId, ConversationTurnState candidate) =>
        _turnStates.GetOrAdd(conversationId, candidate);

    public void SetBridge(string key, GeminiLiveSessionBridge bridge) =>
        _bridges[key] = bridge;

    public bool TryGetBridge(string key, out GeminiLiveSessionBridge? bridge) =>
        _bridges.TryGetValue(key, out bridge);

    public async Task RemoveBridgeIfExistsAsync(string key)
    {
        if (_bridges.TryRemove(key, out var existing))
        {
            await existing.DisposeAsync();
        }
    }

    public CancellationTokenSource ResetTranslationDebounce(string conversationId, string turnId)
    {
        var dict = _translationDebounce.GetOrAdd(conversationId, _ => new Dictionary<string, CancellationTokenSource>(StringComparer.Ordinal));
        lock (dict)
        {
            if (dict.TryGetValue(turnId, out var oldCts))
            {
                try { oldCts.Cancel(); } catch { }
                oldCts.Dispose();
            }
            var cts = new CancellationTokenSource();
            dict[turnId] = cts;
            return cts;
        }
    }

    public CancellationTokenSource ResetGrammarDebounce(string conversationId, string turnId)
    {
        var dict = _grammarDebounce.GetOrAdd(conversationId, _ => new Dictionary<string, CancellationTokenSource>(StringComparer.Ordinal));
        lock (dict)
        {
            if (dict.TryGetValue(turnId, out var oldCts))
            {
                try { oldCts.Cancel(); } catch { }
                oldCts.Dispose();
            }
            var cts = new CancellationTokenSource();
            dict[turnId] = cts;
            return cts;
        }
    }

    public async Task CleanupConversationAsync(string conversationId)
    {
        string prefix = $"{conversationId}:";
        var keys = _bridges.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList();
        foreach (var k in keys)
        {
            await RemoveBridgeIfExistsAsync(k);
        }

        if (_translationDebounce.TryRemove(conversationId, out var transDict))
        {
            lock (transDict)
            {
                foreach (var cts in transDict.Values)
                {
                    try { cts.Cancel(); } catch { }
                    cts.Dispose();
                }
                transDict.Clear();
            }
        }

        if (_grammarDebounce.TryRemove(conversationId, out var gramDict))
        {
            lock (gramDict)
            {
                foreach (var cts in gramDict.Values)
                {
                    try { cts.Cancel(); } catch { }
                    cts.Dispose();
                }
                gramDict.Clear();
            }
        }

        _sessionApiKeys.TryRemove(conversationId, out _);
        _turnStates.TryRemove(conversationId, out _);

        lock (_ownershipLock)
        {
            var bridgeKeys = _bridgeConnections.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList();
            foreach (var bk in bridgeKeys)
            {
                _bridgeConnections.TryRemove(bk, out _);
            }

            var connIds = _connectionToConversation
                .Where(kv => string.Equals(kv.Value, conversationId, StringComparison.Ordinal))
                .Select(kv => kv.Key)
                .ToList();
            foreach (var cid in connIds)
            {
                _connectionToConversation.TryRemove(cid, out _);
            }
        }
    }

    public bool HasAnyBridgeForConversation(string conversationId)
    {
        string prefix = $"{conversationId}:";
        return _bridges.Keys.Any(k => k.StartsWith(prefix, StringComparison.Ordinal));
    }
}
