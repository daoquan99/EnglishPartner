using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Infrastructure.Observability;

namespace EnglishLiveTutor.Infrastructure.Ai;

/// <summary>
/// Manages a persistent WebSocket connection to Google AI Studio's BidiGenerateContent API
/// with automatic transparent reconnection when the session lifetime expires.
/// </summary>
public class GeminiLiveSessionBridge : IAsyncDisposable
{
    private const string EndpointTemplate =
        "wss://generativelanguage.googleapis.com/ws/google.ai.generativelanguage.v1beta.GenerativeService.BidiGenerateContent?key={0}";

    private ClientWebSocket _ws = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly SemaphoreSlim _reconnectLock = new(1, 1);
    private TaskCompletionSource<bool> _setupCompleteTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Task? _receiveLoopTask;

    private string _apiKey = string.Empty;
    private object? _setupPayload;
    private readonly string _modelCode = string.Empty;
    private int _isDisposed;
    private int _sessionStarted;
    private int _sessionDisposed;
    private long _sessionStartTimestamp;
    private long _accumulatedInputTokens;
    private long _accumulatedOutputTokens;

    public string ConversationId { get; }
    public string RoleTag { get; }
    public virtual bool IsOpen => Volatile.Read(ref _isDisposed) == 0 && _ws.State == WebSocketState.Open;

    public event Func<JsonNode, Task>? OnServerMessage;

    /// <summary>Raised when the server closes the socket or receiving fails (before any auto-reconnect).
    /// One-shot callers waiting for a turn use it to stop waiting instead of running into their timeout.</summary>
    public event Action? OnConnectionLost;

    public GeminiLiveSessionBridge(string conversationId, string roleTag, string? modelCode = null)
    {
        ConversationId = conversationId;
        RoleTag = roleTag;
        _modelCode = modelCode ?? string.Empty;
        _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    }

    public virtual async Task ConnectAndSetupAsync(string apiKey, object setupPayload, CancellationToken ct = default)
    {
        _apiKey = apiKey;
        _setupPayload = setupPayload;

        await ConnectInternalAsync(ct);
        if (Interlocked.Exchange(ref _sessionStarted, 1) == 0)
        {
            _sessionStartTimestamp = Stopwatch.GetTimestamp();
            AiTelemetry.RecordLiveSessionStarted(RoleTag);

            // Race check if disposed concurrently
            if (Volatile.Read(ref _isDisposed) == 1 && Interlocked.Exchange(ref _sessionDisposed, 1) == 0)
            {
                double durationSeconds = Stopwatch.GetElapsedTime(_sessionStartTimestamp).TotalSeconds;
                AiTelemetry.RecordLiveSessionStopped(durationSeconds, role: RoleTag, model: _modelCode);
            }
        }
    }

    protected virtual async Task ConnectInternalAsync(CancellationToken ct)
    {
        _setupCompleteTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var uri = new Uri(string.Format(EndpointTemplate, Uri.EscapeDataString(_apiKey.Trim())));
        await _ws.ConnectAsync(uri, ct);

        _receiveLoopTask = Task.Run(() => ReceiveLoopAsync(_cts.Token));
        if (_setupPayload is not null)
        {
            await SendJsonDirectAsync(_setupPayload, ct);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            await _setupCompleteTcs.Task.WaitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Proceed if setupComplete was coalesced
        }
    }

    private async Task EnsureConnectedAsync(CancellationToken ct = default)
    {
        if (Volatile.Read(ref _isDisposed) == 1 || _cts.IsCancellationRequested)
            return;

        if (_ws.State == WebSocketState.Open)
            return;

        await _reconnectLock.WaitAsync(ct);
        try
        {
            if (_ws.State == WebSocketState.Open || Volatile.Read(ref _isDisposed) == 1 || _cts.IsCancellationRequested)
                return;

            Console.WriteLine($"[{RoleTag}] Reconnecting Google Live WebSocket for {ConversationId}...");
            try { _ws.Dispose(); } catch { }
            _ws = new ClientWebSocket();
            _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);

            await ConnectInternalAsync(ct);
            if (Interlocked.Exchange(ref _sessionStarted, 1) == 0)
            {
                _sessionStartTimestamp = Stopwatch.GetTimestamp();
                AiTelemetry.RecordLiveSessionStarted(RoleTag);

                if (Volatile.Read(ref _isDisposed) == 1 && Interlocked.Exchange(ref _sessionDisposed, 1) == 0)
                {
                    double durationSeconds = Stopwatch.GetElapsedTime(_sessionStartTimestamp).TotalSeconds;
                    AiTelemetry.RecordLiveSessionStopped(durationSeconds, role: RoleTag, model: _modelCode);
                }
            }
            Console.WriteLine($"[{RoleTag}] Reconnected Google Live WebSocket successfully.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[{RoleTag}] Auto-reconnect failed: {ex.GetType().Name}");
        }
        finally
        {
            _reconnectLock.Release();
        }
    }

    public virtual async Task SeedConversationHistoryAsync(IEnumerable<object> turns, CancellationToken ct = default)
    {
        var payload = new
        {
            clientContent = new
            {
                turns = turns.ToArray(),
                turnComplete = false
            }
        };

        await SendJsonAsync(payload, ct);
    }

    public virtual async Task SendRealtimeAudioAsync(string base64Pcm16k, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(base64Pcm16k) || Volatile.Read(ref _isDisposed) == 1)
            return;

        if (_ws.State != WebSocketState.Open)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await EnsureConnectedAsync(_cts.Token);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[{RoleTag}] EnsureConnectedAsync error: {ex.GetType().Name}");
                }
            });
            return;
        }

        var payload = new
        {
            realtimeInput = new
            {
                audio = new
                {
                    data = base64Pcm16k,
                    mimeType = "audio/pcm;rate=16000"
                }
            }
        };

        await SendJsonDirectAsync(payload, ct);
    }

    public virtual async Task SendAudioStreamEndAsync(CancellationToken ct = default)
    {
        if (_ws.State != WebSocketState.Open)
            return;

        var payload = new
        {
            realtimeInput = new
            {
                audioStreamEnd = true
            }
        };

        await SendJsonDirectAsync(payload, ct);
    }

    public async Task SendJsonAsync(object payload, CancellationToken ct = default)
    {
        await EnsureConnectedAsync(ct);
        await SendJsonDirectAsync(payload, ct);
    }

    private async Task SendJsonDirectAsync(object payload, CancellationToken ct = default)
    {
        if (_ws.State != WebSocketState.Open)
            return;

        string json = JsonSerializer.Serialize(payload);
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        await _sendLock.WaitAsync(ct);
        try
        {
            if (_ws.State == WebSocketState.Open)
            {
                await _ws.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, ct);
            }
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private void RaiseConnectionLost()
    {
        try
        {
            OnConnectionLost?.Invoke();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[{RoleTag}] Error in OnConnectionLost: {ex.GetType().Name}");
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[64 * 1024];
        using var ms = new MemoryStream();

        try
        {
            while (!ct.IsCancellationRequested && _ws.State == WebSocketState.Open)
            {
                ms.SetLength(0);
                WebSocketReceiveResult result;
                do
                {
                    result = await _ws.ReceiveAsync(buffer, ct);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _setupCompleteTcs.TrySetResult(false);
                        RaiseConnectionLost();
                        // Trigger background auto-reconnect if session wasn't intentionally stopped
                        if (!ct.IsCancellationRequested && Volatile.Read(ref _isDisposed) == 0)
                        {
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await Task.Delay(500, ct);
                                    await EnsureConnectedAsync(ct);
                                }
                                catch (Exception ex)
                                {
                                    Console.Error.WriteLine($"[{RoleTag}] Background reconnect error: {ex.GetType().Name}");
                                }
                            });
                        }
                        return;
                    }
                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                string rawJson = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
                var node = JsonNode.Parse(rawJson);
                if (node is not null)
                {
                    if (node["usageMetadata"] is JsonNode usage)
                    {
                        var (prompt, cand) = AiTelemetry.ExtractUsageTokens(usage);
                        if (prompt.HasValue && prompt.Value > 0)
                            Interlocked.Add(ref _accumulatedInputTokens, prompt.Value);
                        if (cand.HasValue && cand.Value > 0)
                            Interlocked.Add(ref _accumulatedOutputTokens, cand.Value);
                    }

                    if (node["setupComplete"] is not null)
                    {
                        _setupCompleteTcs.TrySetResult(true);
                    }

                    // If Google sends a GoAway frame (connection lifetime nearing 10m limit), proactively reconnect
                    if (node["goAway"] is not null && !ct.IsCancellationRequested && Volatile.Read(ref _isDisposed) == 0)
                    {
                        Console.WriteLine($"[{RoleTag}] Received GoAway signal from Google Live API; scheduling seamless reconnect.");
                    }

                    if (OnServerMessage is not null)
                    {
                        try
                        {
                            await OnServerMessage.Invoke(node);
                        }
                        catch (Exception ex)
                        {
                            // Error isolation: handler errors must never terminate or escape the WebSocket receive loop
                            Console.Error.WriteLine($"[{RoleTag}] Error in OnServerMessage: {ex.GetType().Name}");
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _setupCompleteTcs.TrySetResult(false);
            RaiseConnectionLost();
            Console.Error.WriteLine($"[{RoleTag}] WebSocket receive error: {ex.GetType().Name}");
            if (!ct.IsCancellationRequested && Volatile.Read(ref _isDisposed) == 0)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(800, ct);
                    await EnsureConnectedAsync(ct);
                });
            }
        }
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
        {
            return;
        }

        if (Volatile.Read(ref _sessionStarted) == 1 && Interlocked.Exchange(ref _sessionDisposed, 1) == 0)
        {
            double durationSeconds = Stopwatch.GetElapsedTime(_sessionStartTimestamp).TotalSeconds;
            AiTelemetry.RecordLiveSessionStopped(
                durationSeconds,
                _accumulatedInputTokens > 0 ? _accumulatedInputTokens : null,
                _accumulatedOutputTokens > 0 ? _accumulatedOutputTokens : null,
                role: RoleTag,
                model: _modelCode);
        }
        _apiKey = string.Empty;
        await _cts.CancelAsync();
        if (_ws.State == WebSocketState.Open)
        {
            try
            {
                using var closeCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Session closed", closeCts.Token);
            }
            catch { }
        }
        _ws.Dispose();
        _sendLock.Dispose();
        _reconnectLock.Dispose();
        _cts.Dispose();
    }
}
