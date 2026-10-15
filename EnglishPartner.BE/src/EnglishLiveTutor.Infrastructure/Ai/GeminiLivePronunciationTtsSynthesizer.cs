using System.Text;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Infrastructure.Realtime;

namespace EnglishLiveTutor.Infrastructure.Ai;

/// <summary>
/// Synthesizes high-fidelity spoken pronunciation audio (24kHz WAV) for a Vocabulary word
/// or Example sentence using Google's "gemini-3.8-live" or "gemini-3.8-live-extended-thinking"
/// model, one of the 30 Google prebuilt Teacher Voices, and US/UK accent instructions.
/// </summary>
public sealed class GeminiLivePronunciationTtsSynthesizer : ILivePronunciationTtsSynthesizer
{
    public async Task<SynthesizePronunciationAudioResponseDto> SynthesizeSpeechAsync(
        SynthesizePronunciationAudioRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
            throw new InvalidOperationException("Vui lòng nhập Google AI Studio API Key để tạo âm thanh phát âm.");
        if (string.IsNullOrWhiteSpace(request.Text))
            throw new InvalidOperationException("Nội dung từ vựng hoặc câu mẫu cần phát âm không được để trống.");

        string normalizedModel = (request.ModelCode ?? "gemini-3.8-live").Trim().ToLowerInvariant();
        bool useExtendedThinking = normalizedModel.Contains("thinking");
        string fullModelId = useExtendedThinking
            ? "models/gemini-3.8-live-extended-thinking"
            : "models/gemini-3.8-live";
        string effectiveModelCode = useExtendedThinking
            ? "gemini-3.8-live-extended-thinking"
            : "gemini-3.8-live";

        string accent = string.Equals(request.Accent?.Trim(), "UK", StringComparison.OrdinalIgnoreCase)
            ? "UK"
            : "US";
        string accentFullDescription = accent == "UK"
            ? "British English (Received Pronunciation / Standard UK accent)"
            : "American English (General American / Standard US accent)";

        string voiceName = string.IsNullOrWhiteSpace(request.VoiceName) ? "Aoede" : request.VoiceName.Trim();
        double speed = Math.Clamp(request.SpeechSpeed <= 0 ? 1.0 : request.SpeechSpeed, 0.5, 2.0);
        string targetType = string.Equals(request.TargetType, "sentence", StringComparison.OrdinalIgnoreCase)
            ? "sentence"
            : "word";

        string systemPrompt = string.Join("\n", new[]
        {
            $"You are Teacher {voiceName}, a professional English pronunciation model for an English dictionary and speaking platform.",
            $"TARGET ACCENT & INTONATION: Speak strictly with a natural, authentic {accentFullDescription}.",
            $"SPEECH PACING: Deliver the speech at {speed:0.00}x speed with crystal-clear phonetics, accurate word stress, and natural rhythm.",
            "STRICT OUTPUT RULES:",
            "1. Pronounce ONLY the exact English word or example sentence requested by the user.",
            "2. NEVER add any introductory words, greetings, explanations, or filler phrases (do NOT say 'Sure', 'Here is the pronunciation', or 'The word is').",
            "3. Pronounce the target text once, clearly and naturally, then stop immediately."
        });

        var generationConfig = new Dictionary<string, object>
        {
            ["responseModalities"] = new[] { "AUDIO" },
            ["speechConfig"] = new
            {
                voiceConfig = new
                {
                    prebuiltVoiceConfig = new
                    {
                        voiceName
                    }
                }
            }
        };

        if (useExtendedThinking)
        {
            generationConfig["thinkingConfig"] = new
            {
                thinkingLevel = "low"
            };
        }

        var setupPayload = new
        {
            setup = new
            {
                model = fullModelId,
                generationConfig,
                systemInstruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                }
            }
        };

        await using var bridge = new GeminiLiveSessionBridge($"tts_{Guid.NewGuid():N}", "tts-synth");
        using var pcmStream = new MemoryStream();
        var turnCompleteTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        bridge.OnServerMessage += (JsonNode node) =>
        {
            var serverContent = node["serverContent"];
            if (serverContent is not null)
            {
                var parts = serverContent["modelTurn"]?["parts"]?.AsArray();
                if (parts is not null)
                {
                    foreach (var part in parts)
                    {
                        string? base64Pcm24k = part?["inlineData"]?["data"]?.GetValue<string>();
                        if (!string.IsNullOrEmpty(base64Pcm24k))
                        {
                            try
                            {
                                byte[] chunk = Convert.FromBase64String(base64Pcm24k);
                                lock (pcmStream)
                                {
                                    pcmStream.Write(chunk, 0, chunk.Length);
                                }
                            }
                            catch
                            {
                                // ignore malformed chunk
                            }
                        }
                    }
                }

                if (serverContent["turnComplete"]?.GetValue<bool>() == true)
                {
                    turnCompleteTcs.TrySetResult(true);
                }
            }
            return Task.CompletedTask;
        };

        await bridge.ConnectAndSetupAsync(request.ApiKey.Trim(), setupPayload, ct);

        string userPrompt = targetType == "word"
            ? $"Pronounce this exact English word in {accent} accent (say ONLY the word itself): {request.Text.Trim()}"
            : $"Pronounce this exact English sentence in {accent} accent (say ONLY the sentence itself): {request.Text.Trim()}";

        var clientTurnPayload = new
        {
            clientContent = new
            {
                turns = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = userPrompt } }
                    }
                },
                turnComplete = true
            }
        };

        await bridge.SendJsonAsync(clientTurnPayload, ct);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(18));
        bool turnCompleted = false;
        try
        {
            await turnCompleteTcs.Task.WaitAsync(timeoutCts.Token);
            turnCompleted = true;
        }
        catch (OperationCanceledException)
        {
            if (ct.IsCancellationRequested)
            {
                throw;
            }
            // If we already received audio chunks before internal timeout, return them without caching
        }

        byte[] rawPcmBytes;
        lock (pcmStream)
        {
            rawPcmBytes = pcmStream.ToArray();
        }

        if (rawPcmBytes.Length == 0)
        {
            throw new InvalidOperationException("Gemini Live không trả về dữ liệu âm thanh. Vui lòng kiểm tra lại API Key hoặc thử lại.");
        }

        byte[] wavBytes = WrapPcm16MonoInWavContainer(rawPcmBytes, sampleRate: 24000);
        int durationMs = (int)Math.Max(200, Math.Round(rawPcmBytes.Length / (24000.0 * 2.0) * 1000.0));

        return new SynthesizePronunciationAudioResponseDto(
            Text: request.Text.Trim(),
            TargetType: targetType,
            Accent: accent,
            VoiceName: voiceName,
            SpeechSpeed: speed,
            ModelCodeUsed: effectiveModelCode,
            SystemPromptUsed: systemPrompt,
            MimeType: "audio/wav",
            SampleRate: 24000,
            DurationMs: durationMs,
            AudioBase64: Convert.ToBase64String(wavBytes),
            IsComplete: turnCompleted
        );
    }

    public static byte[] WrapPcm16MonoInWavContainer(byte[] pcmBytes, int sampleRate = 24000)
    {
        const short numChannels = 1;
        const short bitsPerSample = 16;
        int byteRate = sampleRate * numChannels * (bitsPerSample / 8);
        short blockAlign = (short)(numChannels * (bitsPerSample / 8));
        int subchunk2Size = pcmBytes.Length;
        int chunkSize = 36 + subchunk2Size;

        using var ms = new MemoryStream(44 + pcmBytes.Length);
        using var bw = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true);

        // RIFF header
        bw.Write(Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(chunkSize);
        bw.Write(Encoding.ASCII.GetBytes("WAVE"));

        // fmt subchunk
        bw.Write(Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16); // Subchunk1Size for PCM
        bw.Write((short)1); // AudioFormat 1 = PCM
        bw.Write(numChannels);
        bw.Write(sampleRate);
        bw.Write(byteRate);
        bw.Write(blockAlign);
        bw.Write(bitsPerSample);

        // data subchunk
        bw.Write(Encoding.ASCII.GetBytes("data"));
        bw.Write(subchunk2Size);
        bw.Write(pcmBytes);
        bw.Flush();

        return ms.ToArray();
    }
}
