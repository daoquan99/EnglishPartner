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
    private bool _isDisposed;

    public Func<byte[], Task>? OnAudioReceived { get; set; }
    public Func<string, Task>? OnTextReceived { get; set; }

    public GoogleGenAILiveAudioSession(
        IOptions<GeminiAISettings> settings,
        ILogger<GoogleGenAILiveAudioSession> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    private Client CreateClient()
    {
        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            return new Client(apiKey: _settings.ApiKey);
        }

        if (!string.IsNullOrWhiteSpace(_settings.ProjectId))
        {
            return new Client(vertexAI: true, project: _settings.ProjectId, location: _settings.Location);
        }

        throw new InvalidOperationException("ApiKey or ProjectId must be configured.");
    }

    public async Task ConnectAsync(string systemInstruction, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        
        var config = new LiveConnectConfig
        {
            GenerationConfig = new GenerationConfig
            {
                Temperature = (float)_settings.Temperature,
                SpeechConfig = new SpeechConfig
                {
                    VoiceConfig = new VoiceConfig
                    {
                        PrebuiltVoiceConfig = new PrebuiltVoiceConfig
                        {
                            VoiceName = LiveVoiceCatalog.GetValidVoiceOrDefault(_settings.VoiceName)
                        }
                    }
                }
            },
            SystemInstruction = new Content { Role = "system", Parts = [new Part { Text = systemInstruction }] },
            ResponseModalities = ["AUDIO", "TEXT"] // Ensure synchronized audio & text
        };

        // Note: Live models must support the Live API
        var modelName = !string.IsNullOrWhiteSpace(_settings.LiveModelName) 
            ? _settings.LiveModelName 
            : "gemini-live-2.5-flash-native-audio";

        _sessionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        _logger.LogInformation("[GoogleGenAILive] Connecting to Live API using model {Model}", modelName);
        
        _session = await client.Live.ConnectAsync(modelName, config, cancellationToken);
        
        // Start background receive loop
        _receiveLoopTask = Task.Run(() => ReceiveLoopAsync(_sessionCts.Token));
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        if (_session is null) return;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var message = await _session.ReceiveAsync(cancellationToken);
                if (message == null) continue;

                var content = message.ServerContent;
                if (content?.ModelTurn?.Parts != null)
                {
                    foreach (var part in content.ModelTurn.Parts)
                    {
                        // Output audio
                        if (part.InlineData?.Data != null && OnAudioReceived != null)
                        {
                            await OnAudioReceived(part.InlineData.Data);
                        }

                        // Output text
                        if (!string.IsNullOrEmpty(part.Text) && OnTextReceived != null)
                        {
                            await OnTextReceived(part.Text);
                        }
                    }
                }

                if (content?.TurnComplete == true)
                {
                    // Signal turn complete if needed
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GoogleGenAILive] Error in receive loop.");
        }
    }

    public async Task SendAudioAsync(byte[] pcmData, CancellationToken cancellationToken = default)
    {
        if (_session is null) throw new InvalidOperationException("Session is not connected.");

        var parameters = new LiveSendRealtimeInputParameters
        {
            Media = new Blob
            {
                MimeType = "audio/pcm;rate=16000",
                Data = pcmData
            }
        };

        await _session.SendRealtimeInputAsync(parameters, cancellationToken);
    }

    public async Task SendTextAsync(string text, CancellationToken cancellationToken = default)
    {
        if (_session is null) throw new InvalidOperationException("Session is not connected.");

        var parameters = new LiveSendClientContentParameters
        {
            Turns = [
                new Content
                {
                    Role = "user",
                    Parts = [new Part { Text = text }]
                }
            ]
        };

        await _session.SendClientContentAsync(parameters, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_sessionCts != null)
        {
            _sessionCts.Cancel();
            _sessionCts.Dispose();
        }

        if (_session != null)
        {
            try
            {
                await _session.CloseAsync();
                await _session.DisposeAsync();
            }
            catch { /* Ignore close errors on dispose */ }
        }

        if (_receiveLoopTask != null)
        {
            try { await _receiveLoopTask; }
            catch { /* ignore */ }
        }
    }
}
