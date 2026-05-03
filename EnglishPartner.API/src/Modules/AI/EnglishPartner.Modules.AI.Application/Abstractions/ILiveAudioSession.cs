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
    /// Event triggered when the user's speech is transcribed.
    /// </summary>
    Func<string, Task>? OnInputTranscriptionReceived { get; set; }

    /// <summary>
    /// Event triggered when the AI's speech is transcribed.
    /// </summary>
    Func<string, Task>? OnOutputTranscriptionReceived { get; set; }

    /// <summary>
    /// Event triggered when the AI finishes its turn (stops speaking).
    /// </summary>
    Func<Task>? OnTurnComplete { get; set; }

    /// <summary>
    /// Event triggered when a session-level error occurs.
    /// </summary>
    Func<string, Task>? OnError { get; set; }

    /// <summary>
    /// Connects to the Live API session.
    /// </summary>
    /// <param name="systemInstruction">System instruction for the AI.</param>
    /// <param name="voiceName">Optional voice name from LiveVoiceCatalog (e.g., "Zephyr"). Falls back to settings default.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ConnectAsync(string systemInstruction, string? voiceName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams raw PCM audio chunk (usually 16-bit at 16kHz) to the AI.
    /// </summary>
    Task SendAudioAsync(byte[] pcmData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a text message or interruption flag to the AI.
    /// </summary>
    Task SendTextAsync(string text, CancellationToken cancellationToken = default);
}
