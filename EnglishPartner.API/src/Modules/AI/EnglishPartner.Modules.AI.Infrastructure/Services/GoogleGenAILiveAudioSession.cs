using System.Text;
using EnglishPartner.Modules.AI.Application.Abstractions;
using EnglishPartner.SharedKernel.Settings;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishPartner.Modules.AI.Infrastructure.Services;

internal sealed class GoogleGenAILiveAudioSession : ILiveAudioSession
{
    private readonly GeminiAISettings _settings;
    private readonly ILogger<GoogleGenAILiveAudioSession> _logger;
    private AsyncSession? _session;
    private Task? _receiveLoopTask;
    private CancellationTokenSource? _sessionCts;
    private readonly SemaphoreSlim _sync = new(1, 1);
    private string? _lastSystemInstruction;
    private string? _lastVoiceName;
    private bool _isDisposed;

    public Func<byte[], Task>? OnAudioReceived { get; set; }
    public Func<string, Task>? OnTextReceived { get; set; }
    public Func<string, Task>? OnInputTranscriptionReceived { get; set; }
    public Func<string, Task>? OnOutputTranscriptionReceived { get; set; }
    public Func<Task>? OnTurnComplete { get; set; }
    public Func<string, Task>? OnError { get; set; }

    public GoogleGenAILiveAudioSession(
        IOptions<GeminiAISettings> settings,
        ILogger<GoogleGenAILiveAudioSession> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    private Client CreateClient()
    {
        if (!string.IsNullOrWhiteSpace(_settings.LiveApiKey))
        {
            return new Client(apiKey: _settings.LiveApiKey);
        }

        if (!string.IsNullOrWhiteSpace(_settings.ProjectId))
        {
            return new Client(vertexAI: true, project: _settings.ProjectId, location: _settings.Location);
        }

        throw new InvalidOperationException("ApiKey or ProjectId must be configured in GeminiAI settings.");
    }

    public async Task ConnectAsync(string systemInstruction, string? voiceName = null, CancellationToken cancellationToken = default)
    {
        _lastSystemInstruction = systemInstruction;
        _lastVoiceName = voiceName;
        await ReconnectAsync(cancellationToken);
    }

    public async Task ReconnectAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_lastSystemInstruction))
        {
            throw new InvalidOperationException("No prior system instruction found. Call ConnectAsync first.");
        }

        await _sync.WaitAsync(cancellationToken);
        try
        {
            await CloseSessionInternalAsync();

            var client = CreateClient();
            
            var config = new LiveConnectConfig
            {
                Temperature = (float)_settings.Temperature,
                SpeechConfig = new SpeechConfig
                {
                    VoiceConfig = new VoiceConfig
                    {
                        PrebuiltVoiceConfig = new PrebuiltVoiceConfig
                        {
                            VoiceName = LiveVoiceCatalog.GetValidVoiceOrDefault(_lastVoiceName ?? _settings.VoiceName)
                        }
                    }
                },
                SystemInstruction = new Content { Role = "system", Parts = [new Part { Text = _lastSystemInstruction }] },
                ResponseModalities = ["AUDIO"], // Frontend expects only AUDIO modality generally, text comes via transcriptions
                InputAudioTranscription = new AudioTranscriptionConfig(),
                OutputAudioTranscription = new AudioTranscriptionConfig()
            };

            // Note: Live models must support the Live API
            var modelName = !string.IsNullOrWhiteSpace(_settings.LiveModelName) 
                ? _settings.LiveModelName 
                : "models/gemini-2.5-flash-live";

            // Ensure models/ prefix for API key mode
            if (!modelName.StartsWith("models/", StringComparison.OrdinalIgnoreCase))
            {
                modelName = $"models/{modelName}";
            }

            _sessionCts = new CancellationTokenSource();
            
            _logger.LogInformation("[GoogleGenAILive] Connecting to Live API using model {Model}", modelName);
            
            _session = await client.Live.ConnectAsync(modelName, config, cancellationToken);
            
            _logger.LogInformation("[GoogleGenAILive] ✅ Connected to Live API successfully. Starting receive loop.");
            _receiveLoopTask = Task.Run(() => ReceiveLoopAsync(_sessionCts.Token));
        }
        finally
        {
            _sync.Release();
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        if (_session is null) return;

        _logger.LogInformation("[GoogleGenAILive] Receive loop started.");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var message = await _session.ReceiveAsync(cancellationToken);
                if (message == null)
                {
                    _logger.LogDebug("[GoogleGenAILive] Received null message, continuing...");
                    continue;
                }

                // Log raw message structure for diagnostics
                var content = message.ServerContent;
                var toolCall = message.ToolCall;
                var setupComplete = message.SetupComplete;

                _logger.LogDebug(
                    "[GoogleGenAILive] Message received — ServerContent={HasContent}, ToolCall={HasTool}, SetupComplete={SetupComplete}",
                    content != null, toolCall != null, setupComplete != null);

                if (setupComplete != null)
                {
                    _logger.LogInformation("[GoogleGenAILive] ✅ Setup complete — session is ready for input.");
                }

                if (content?.ModelTurn?.Parts != null)
                {
                    _logger.LogDebug("[GoogleGenAILive] ModelTurn has {Count} parts.", content.ModelTurn.Parts.Count);

                    foreach (var part in content.ModelTurn.Parts)
                    {
                        // Output audio
                        if (part.InlineData?.Data != null)
                        {
                            _logger.LogDebug("[GoogleGenAILive] 🔊 Audio chunk received: {Bytes} bytes, MimeType={Mime}",
                                part.InlineData.Data.Length, part.InlineData.MimeType ?? "null");

                            if (OnAudioReceived != null)
                            {
                                await OnAudioReceived(part.InlineData.Data);
                            }
                        }

                        // Output text (fallback if transcriptions aren't complete)
                        if (!string.IsNullOrEmpty(part.Text))
                        {
                            _logger.LogDebug("[GoogleGenAILive] 📝 Text part received: \"{Text}\"",
                                part.Text.Length > 100 ? part.Text[..100] + "..." : part.Text);

                            if (OnTextReceived != null)
                            {
                                await OnTextReceived(part.Text);
                            }
                        }
                    }
                }

                if (content?.InputTranscription?.Text != null)
                {
                    _logger.LogDebug("[GoogleGenAILive] 🎤 Input transcription: \"{Text}\"", content.InputTranscription.Text);

                    if (OnInputTranscriptionReceived != null)
                    {
                        await OnInputTranscriptionReceived(content.InputTranscription.Text);
                    }
                }

                if (content?.OutputTranscription?.Text != null)
                {
                    _logger.LogDebug("[GoogleGenAILive] 🤖 Output transcription: \"{Text}\"", content.OutputTranscription.Text);

                    if (OnOutputTranscriptionReceived != null)
                    {
                        await OnOutputTranscriptionReceived(content.OutputTranscription.Text);
                    }
                }

                if (content?.TurnComplete == true)
                {
                    _logger.LogInformation("[GoogleGenAILive] ✅ Turn complete.");
                    if (OnTurnComplete != null)
                    {
                        await OnTurnComplete();
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[GoogleGenAILive] Receive loop cancelled (normal shutdown).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GoogleGenAILive] Error in receive loop.");
            if (OnError != null)
            {
                try { await OnError(ex.Message); }
                catch { /* Ignore callback errors */ }
            }
        }
    }

    public async Task SendAudioAsync(byte[] pcmData, CancellationToken cancellationToken = default)
    {
        if (_session is null) throw new InvalidOperationException("Session is not connected.");

        var parameters = new LiveSendRealtimeInputParameters
        {
            Audio = new Blob
            {
                MimeType = "audio/pcm;rate=16000",
                Data = pcmData
            }
        };

        try
        {
            await _session.SendRealtimeInputAsync(parameters, cancellationToken);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("WebSocket is not open", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("[GoogleGenAILive] WebSocket closed. Attempting reconnect...");
            await ReconnectAsync(cancellationToken);
            if (_session is null) throw;

            await _session.SendRealtimeInputAsync(parameters, cancellationToken);
        }
    }

    public async Task SendTextAsync(string text, CancellationToken cancellationToken = default)
    {
        if (_session is null) throw new InvalidOperationException("Session is not connected.");

        _logger.LogInformation("[GoogleGenAILive] 📤 Sending text: \"{Text}\"", 
            text.Length > 100 ? text[..100] + "..." : text);

        var parameters = new LiveSendRealtimeInputParameters
        {
            Text = text
        };

        try
        {
            await _session.SendRealtimeInputAsync(parameters, cancellationToken);
            _logger.LogDebug("[GoogleGenAILive] Text sent successfully, waiting for response...");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("WebSocket is not open", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("[GoogleGenAILive] WebSocket closed. Attempting reconnect...");
            await ReconnectAsync(cancellationToken);
            if (_session is null) throw;

            await _session.SendRealtimeInputAsync(parameters, cancellationToken);
        }
    }

    private async Task CloseSessionInternalAsync()
    {
        if (_sessionCts != null)
        {
            _sessionCts.Cancel();
            _sessionCts.Dispose();
            _sessionCts = null;
        }

        if (_session != null)
        {
            try
            {
                await _session.CloseAsync();
                await _session.DisposeAsync();
            }
            catch { /* Ignore close errors on dispose */ }
            _session = null;
        }

        if (_receiveLoopTask != null)
        {
            try { await _receiveLoopTask; }
            catch { /* ignore */ }
            _receiveLoopTask = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        await CloseSessionInternalAsync();
        _sync.Dispose();
    }
}
