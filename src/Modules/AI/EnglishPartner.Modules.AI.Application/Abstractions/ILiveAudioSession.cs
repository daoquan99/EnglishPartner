namespace EnglishPartner.Modules.AI.Application.Abstractions;

/// <summary>
/// Represents a real-time bidirectional audio/text session with an AI model.
/// </summary>
public interface ILiveAudioSession : IAsyncDisposable
{
    /// <summary>
    /// Event triggered when raw PCM audio chunks are received from the AI model.
    /// Bytes are ordinarily 16-bit PCM at 24kHz.
    /// </summary>
    Func<byte[], Task>? OnAudioReceived { get; set; }

    /// <summary>
    /// Event triggered when text transcriptions or text responses are received.
    /// </summary>
    Func<string, Task>? OnTextReceived { get; set; }

    /// <summary>
    /// Connects to the Live API session.
    /// </summary>
    Task ConnectAsync(string systemInstruction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams raw PCM audio chunk (usually 16-bit at 16kHz) to the AI.
    /// </summary>
    Task SendAudioAsync(byte[] pcmData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a text message or interruption flag to the AI.
    /// </summary>
    Task SendTextAsync(string text, CancellationToken cancellationToken = default);
}
