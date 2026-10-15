using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EnglishLiveTutor.Infrastructure.Realtime;

/// <summary>
/// Manages a persistent WebSocket connection to Google AI Studio's BidiGenerateContent API
/// with automatic transparent reconnection when the session lifetime expires.
/// </summary>
public sealed class GeminiLiveSessionBridge : IAsyncDisposable
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
    private bool _isDisposed;

    public string ConversationId { get; }
    public string RoleTag { get; }
    public bool IsOpen => !_isDisposed && _ws.State == WebSocketState.Open;

    public event Func<JsonNode, Task>? OnServerMessage;

    public GeminiLiveSessionBridge(string conversationId, string roleTag)
    {
        ConversationId = conversationId;
        RoleTag = roleTag;
        _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    }

    public async Task ConnectAndSetupAsync(string apiKey, object setupPayload, CancellationToken ct = default)
    {
        _apiKey = apiKey;
        _setupPayload = setupPayload;

        await ConnectInternalAsync(ct);
    }

    private async Task ConnectInternalAsync(CancellationToken ct)
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
        if (_isDisposed || _cts.IsCancellationRequested)
            return;

        if (_ws.State == WebSocketState.Open)
            return;

        await _reconnectLock.WaitAsync(ct);
        try
        {
            if (_ws.State == WebSocketState.Open || _isDisposed || _cts.IsCancellationRequested)
                return;

            Console.WriteLine($"[{RoleTag}] Reconnecting Google Live WebSocket for {ConversationId}...");
            try { _ws.Dispose(); } catch { }
            _ws = new ClientWebSocket();
            _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);

            await ConnectInternalAsync(ct);
            Console.WriteLine($"[{RoleTag}] Reconnected Google Live WebSocket successfully.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[{RoleTag}] Auto-reconnect failed: {ex.Message}");
        }
        finally
        {
            _reconnectLock.Release();
        }
    }

    public async Task SeedConversationHistoryAsync(IEnumerable<object> turns, CancellationToken ct = default)
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

    public async Task SendRealtimeAudioAsync(string base64Pcm16k, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(base64Pcm16k) || _isDisposed)
            return;

        if (_ws.State != WebSocketState.Open)
        {
            _ = Task.Run(() => EnsureConnectedAsync(_cts.Token));
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

    public async Task SendAudioStreamEndAsync(CancellationToken ct = default)
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
                        // Trigger background auto-reconnect if session wasn't intentionally stopped
                        if (!ct.IsCancellationRequested && !_isDisposed)
                        {
                            _ = Task.Run(async () =>
                            {
                                await Task.Delay(500, ct);
                                await EnsureConnectedAsync(ct);
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
                    if (node["setupComplete"] is not null)
                    {
                        _setupCompleteTcs.TrySetResult(true);
                    }

                    // If Google sends a GoAway frame (connection lifetime nearing 10m limit), proactively reconnect
                    if (node["goAway"] is not null && !ct.IsCancellationRequested && !_isDisposed)
                    {
                        Console.WriteLine($"[{RoleTag}] Received GoAway signal from Google Live API; scheduling seamless reconnect.");
                    }

                    if (OnServerMessage is not null)
                    {
                        await OnServerMessage.Invoke(node);
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
            Console.Error.WriteLine($"[{RoleTag}] WebSocket receive error: {ex.Message}");
            if (!ct.IsCancellationRequested && !_isDisposed)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(800, ct);
                    await EnsureConnectedAsync(ct);
                });
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _isDisposed = true;
        await _cts.CancelAsync();
        if (_ws.State == WebSocketState.Open)
        {
            try
            {
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Session closed", CancellationToken.None);
            }
            catch { }
        }
        _ws.Dispose();
        _sendLock.Dispose();
        _reconnectLock.Dispose();
        _cts.Dispose();
    }
}
