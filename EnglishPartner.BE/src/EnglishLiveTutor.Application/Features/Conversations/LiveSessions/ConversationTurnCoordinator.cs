namespace EnglishLiveTutor.Application.Features.Conversations.LiveSessions;

/// <summary>
/// Holds per-conversation turn sequencing, dual-source ASR transcript buffers,
/// locked Vietnamese translation snapshots, and debounce cancellation tokens.
/// </summary>
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

    public Dictionary<string, string> TranscriptByTurn { get; } = new();
    public Dictionary<string, string> TutorSourceTranscriptByTurn { get; } = new();
    public Dictionary<string, string> TranscribeSourceTranscriptByTurn { get; } = new();
    public Dictionary<string, string> TranslationByTurn { get; } = new();
    public Dictionary<string, string> AudioTranslationBufferByTurn { get; } = new();
    public Dictionary<string, int> SequenceByTurn { get; } = new();
    public Dictionary<string, string> LastAnalyzedTextByTurn { get; } = new();
    public Dictionary<string, CancellationTokenSource> TranslationDebounceByTurn { get; } = new();
    public Dictionary<string, CancellationTokenSource> GrammarDebounceByTurn { get; } = new();
    public List<Task> InFlightTasks { get; } = new();

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

    public Task[] SnapshotActiveTasks()
    {
        lock (SyncRoot)
        {
            InFlightTasks.RemoveAll(t => t.IsCompleted);
            return InFlightTasks.ToArray();
        }
    }

    public void TrackTask(Task task)
    {
        lock (SyncRoot)
        {
            InFlightTasks.RemoveAll(t => t.IsCompleted);
            InFlightTasks.Add(task);
        }
    }

    public void Dispose()
    {
        CancelAndClearDebounces();
    }
}

/// <summary>
/// Pure domain/application turn sequencing and transcript selection policy for realtime conversations.
/// </summary>
public static class ConversationTurnCoordinator
{
    public static (string TurnId, int SequenceOrder) EnsureUserTurn(ConversationTurnState state)
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

    public static (string TurnId, int SequenceOrder) ResolveUserInputTurn(ConversationTurnState state)
    {
        lock (state.SyncRoot)
        {
            if (!string.IsNullOrEmpty(state.ActiveUserTurnId))
            {
                state.UserTurnHasText = true;
                return (state.ActiveUserTurnId, state.ActiveUserSequence);
            }

            if (state.ActiveUserSequence > 0)
            {
                int seq = state.ActiveUserSequence;
                string turnId = state.SequenceByTurn.FirstOrDefault(kv => kv.Value == seq).Key ?? $"u_{seq}";
                return (turnId, seq);
            }

            var created = EnsureUserTurn(state);
            state.UserTurnHasText = true;
            return created;
        }
    }

    public static (string TurnId, int SequenceOrder, string? ClosedUserTurnId) EnsureAiTurn(ConversationTurnState state)
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

    public static (string? ClosedAiTurnId, int SequenceOrder) CompleteAiTurn(ConversationTurnState state)
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
}
