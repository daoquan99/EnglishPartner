using EnglishPartner.Modules.AI.Application.Abstractions;
using EnglishPartner.Modules.AI.Infrastructure.Services;
using EnglishPartner.SharedKernel.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NAudio.Wave;
using Xunit.Abstractions;

namespace EnglishPartner.Infrastructure.Tests.AI;

/// <summary>
/// Integration tests for GoogleGenAILiveAudioSession.
/// Requires GEMINI_LIVE_API_KEY environment variable (or falls back to hardcoded key).
/// 
/// Run:  dotnet test --filter "FullyQualifiedName~LiveAudioSessionTests" -l "console;verbosity=detailed"
/// </summary>
public class LiveAudioSessionTests
{
    private readonly ITestOutputHelper _output;

    private const string DefaultModel = "models/gemini-3.1-flash-live-preview";
    private const string DefaultVoice = "Zephyr";

    public LiveAudioSessionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string GetApiKey()
    {
        return System.Environment.GetEnvironmentVariable("GEMINI_LIVE_API_KEY")
            ?? throw new Exception("API Key NOT FOUND");
    }

    private ILiveAudioSession CreateSession()
    {
        var settings = new GeminiAISettings
        {
            LiveApiKey = GetApiKey(),
            LiveModelName = DefaultModel,
            Temperature = 0.7,
            VoiceName = DefaultVoice
        };

        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(settings));
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddTransient<ILiveAudioSession, GoogleGenAILiveAudioSession>();
        return services.BuildServiceProvider().GetRequiredService<ILiveAudioSession>();
    }

    /// <summary>
    /// Sends a simple text message and verifies we receive audio + transcription response.
    /// </summary>
    [Fact]
    public async Task SendText_ShouldReceiveResponse()
    {
        var session = CreateSession();

        var audioChunks = new List<int>();
        var outputTranscriptions = new List<string>();
        var errors = new List<string>();
        var turnCompleted = new TaskCompletionSource<bool>();

        session.OnAudioReceived = (data) => { audioChunks.Add(data.Length); _output.WriteLine($"[Audio] {data.Length} bytes"); return Task.CompletedTask; };
        session.OnTextReceived = (text) => { _output.WriteLine($"[Text] {text}"); return Task.CompletedTask; };
        session.OnOutputTranscriptionReceived = (text) => { outputTranscriptions.Add(text); _output.WriteLine($"[OutputTranscription] {text}"); return Task.CompletedTask; };
        session.OnInputTranscriptionReceived = (text) => { _output.WriteLine($"[InputTranscription] {text}"); return Task.CompletedTask; };
        session.OnTurnComplete = () => { _output.WriteLine("[TurnComplete]"); turnCompleted.TrySetResult(true); return Task.CompletedTask; };
        session.OnError = (msg) => { errors.Add(msg); _output.WriteLine($"[Error] {msg}"); turnCompleted.TrySetException(new Exception(msg)); return Task.CompletedTask; };

        _output.WriteLine($"Connecting to {DefaultModel}...");
        await session.ConnectAsync("You are a helpful English tutor. Keep responses very short (1-2 sentences).");
        await Task.Delay(1000);

        _output.WriteLine("Sending text: 'Hello, how are you today?'");
        await session.SendTextAsync("Hello, how are you today?");

        _output.WriteLine("Waiting for TurnComplete (up to 25s)...");
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
        var completedInTime = await Task.WhenAny(
            turnCompleted.Task,
            Task.Delay(Timeout.Infinite, timeoutCts.Token)) == turnCompleted.Task;

        _output.WriteLine($"\n--- Results ---");
        _output.WriteLine($"TurnComplete: {completedInTime}");
        _output.WriteLine($"Audio chunks: {audioChunks.Count} ({audioChunks.Sum()} bytes)");
        _output.WriteLine($"Output transcriptions: {outputTranscriptions.Count}");
        _output.WriteLine($"Errors: {errors.Count} — {string.Join("; ", errors)}");

        await session.DisposeAsync();

        errors.Should().BeEmpty();
        completedInTime.Should().BeTrue("AI should complete its turn within 25 seconds");
        (audioChunks.Count > 0 || outputTranscriptions.Count > 0).Should().BeTrue("AI should respond");
    }

    /// <summary>
    /// Reads an MP3 file, converts to 16kHz mono PCM, streams as audio chunks,
    /// and verifies the AI responds with audio and/or transcription.
    /// </summary>
    [Fact]
    public async Task SendAudio_FromMp3File_ShouldReceiveResponse()
    {
        // Arrange
        const string mp3Path = @"D:\Projects\EnglishPartner\EnglishPartner.API\src\Apps\EnglishPartner.Api\uploads\conversations\aae7fddf9340466496106051a6ea57f0\audio\20e89e9bdd084d60b39a5c84e93f6f11_bc45bbb080fd4a8f9d2eee3bc9c4776c.mp3";
        
        if (!File.Exists(mp3Path))
        {
            _output.WriteLine($"SKIP: MP3 file not found at {mp3Path}");
            return;
        }

        // Convert MP3 → 16kHz mono 16-bit PCM
        var pcmData = ConvertMp3ToPcm16kMono(mp3Path);
        _output.WriteLine($"Converted MP3 to PCM: {pcmData.Length} bytes ({pcmData.Length / 32000.0:F1}s of audio)");

        var session = CreateSession();

        var responseAudioChunks = new List<int>();
        var inputTranscriptions = new List<string>();
        var outputTranscriptions = new List<string>();
        var errors = new List<string>();
        var turnCompleted = new TaskCompletionSource<bool>();

        session.OnAudioReceived = (data) =>
        {
            responseAudioChunks.Add(data.Length);
            _output.WriteLine($"[ResponseAudio] {data.Length} bytes");
            return Task.CompletedTask;
        };
        session.OnInputTranscriptionReceived = (text) =>
        {
            inputTranscriptions.Add(text);
            _output.WriteLine($"[YouSaid] {text}");
            return Task.CompletedTask;
        };
        session.OnOutputTranscriptionReceived = (text) =>
        {
            outputTranscriptions.Add(text);
            _output.WriteLine($"[AISaid] {text}");
            return Task.CompletedTask;
        };
        session.OnTurnComplete = () =>
        {
            _output.WriteLine("[TurnComplete]");
            turnCompleted.TrySetResult(true);
            return Task.CompletedTask;
        };
        session.OnError = (msg) =>
        {
            errors.Add(msg);
            _output.WriteLine($"[Error] {msg}");
            turnCompleted.TrySetException(new Exception(msg));
            return Task.CompletedTask;
        };

        // Act — Connect
        _output.WriteLine($"Connecting to {DefaultModel}...");
        await session.ConnectAsync("You are a helpful English tutor. Keep responses very short (1-2 sentences).");
        await Task.Delay(1000);

        // Stream audio in 100ms chunks (3200 bytes = 100ms at 16kHz mono 16-bit)
        const int chunkSize = 3200;
        var totalChunks = 0;

        _output.WriteLine($"Streaming {pcmData.Length} bytes in {chunkSize}-byte chunks...");

        for (var offset = 0; offset < pcmData.Length; offset += chunkSize)
        {
            var size = Math.Min(chunkSize, pcmData.Length - offset);
            var chunk = new byte[size];
            Buffer.BlockCopy(pcmData, offset, chunk, 0, size);
            await session.SendAudioAsync(chunk);
            totalChunks++;
            await Task.Delay(100); // Real-time pacing
        }

        // Send 2 seconds of silence to trigger VAD end-of-speech detection
        _output.WriteLine("Sending 2s of silence to trigger end-of-speech...");
        var silenceChunk = new byte[chunkSize]; // All zeros = silence
        for (var i = 0; i < 20; i++) // 20 × 100ms = 2 seconds
        {
            await session.SendAudioAsync(silenceChunk);
            totalChunks++;
            await Task.Delay(100);
        }

        _output.WriteLine($"Sent {totalChunks} chunks total. Waiting for AI response (up to 30s)...");

        // Wait for TurnComplete
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var completedInTime = await Task.WhenAny(
            turnCompleted.Task,
            Task.Delay(Timeout.Infinite, timeoutCts.Token)) == turnCompleted.Task;

        // Assert
        _output.WriteLine($"\n--- Results ---");
        _output.WriteLine($"TurnComplete: {completedInTime}");
        _output.WriteLine($"Input transcriptions: [{string.Join("", inputTranscriptions)}]");
        _output.WriteLine($"Response audio chunks: {responseAudioChunks.Count} ({responseAudioChunks.Sum()} bytes)");
        _output.WriteLine($"Output transcriptions: [{string.Join("", outputTranscriptions)}]");
        _output.WriteLine($"Errors: {errors.Count} — {string.Join("; ", errors)}");

        await session.DisposeAsync();

        errors.Should().BeEmpty();
        completedInTime.Should().BeTrue("AI should respond to audio input within 30 seconds");
        (responseAudioChunks.Count > 0 || outputTranscriptions.Count > 0).Should().BeTrue("AI should produce a response");
    }

    /// <summary>
    /// Converts an MP3 file to 16kHz mono 16-bit PCM byte array using NAudio.
    /// </summary>
    private static byte[] ConvertMp3ToPcm16kMono(string mp3Path)
    {
        using var mp3Reader = new Mp3FileReader(mp3Path);
        var targetFormat = new WaveFormat(16000, 16, 1);
        using var resampler = new MediaFoundationResampler(mp3Reader, targetFormat)
        {
            ResamplerQuality = 60
        };

        using var ms = new MemoryStream();
        var buffer = new byte[4096];
        int bytesRead;
        while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
        {
            ms.Write(buffer, 0, bytesRead);
        }

        return ms.ToArray();
    }
}
