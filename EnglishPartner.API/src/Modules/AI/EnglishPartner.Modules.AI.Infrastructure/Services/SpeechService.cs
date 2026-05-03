using EnglishPartner.Modules.AI.Application.Abstractions;
using EnglishPartner.SharedKernel.Settings;
using Google.Cloud.Speech.V1;
using Google.Cloud.TextToSpeech.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Grpc.Core;

namespace EnglishPartner.Modules.AI.Infrastructure.Services;

/// <summary>
/// Implements TTS and STT using official Google Cloud client libraries.
/// Auth: Application Default Credentials (ADC) — handled automatically by the SDK.
///   Dev:  gcloud auth application-default login
///   Prod: GOOGLE_APPLICATION_CREDENTIALS=/path/to/service-account.json
/// </summary>
internal sealed class SpeechService(
    IOptions<GeminiAISettings> settings,
    ILogger<SpeechService> logger) : ISpeechService
{
    private GeminiAISettings Config => settings.Value;

    // ── Text-to-Speech ──────────────────────────────────────────────────────

    public async Task<byte[]> SynthesizeSpeechAsync(
        string text,
        string? voice = null,
        double? speakingRate = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedVoice = voice ?? Config.DefaultTtsVoice;
        var resolvedRate = speakingRate ?? Config.DefaultTtsSpeakingRate;
        
        var voiceInfo = TtsVoiceCatalog.FindById(resolvedVoice);
        var languageCode = voiceInfo?.LanguageCode ?? Config.DefaultTtsLanguage;

        logger.LogInformation("[TTS] Synthesizing | voice={Voice} rate={Rate} lang={Lang}", resolvedVoice, resolvedRate, languageCode);

        var client = await TextToSpeechClient.CreateAsync(cancellationToken);

        var request = new SynthesizeSpeechRequest
        {
            Input = new SynthesisInput { Text = text },
            Voice = new VoiceSelectionParams
            {
                LanguageCode = languageCode,
                Name = resolvedVoice
            },
            AudioConfig = new AudioConfig
            {
                AudioEncoding = AudioEncoding.Mp3,
                SpeakingRate = resolvedRate
            }
        };

        var response = await client.SynthesizeSpeechAsync(request, cancellationToken);

        var audioBytes = response.AudioContent.ToByteArray();
        logger.LogInformation("[TTS] Synthesized {Bytes} bytes of MP3 audio.", audioBytes.Length);
        return audioBytes;
    }

    // ── Speech-to-Text ──────────────────────────────────────────────────────

    public async Task<SttResult> RecognizeSpeechAsync(
        byte[] audioData,
        string mimeType,
        IReadOnlyList<string>? languageCodes = null,
        CancellationToken cancellationToken = default)
    {
        // Resolve and normalize all language codes
        var resolved = (languageCodes is { Count: > 0 }
            ? languageCodes
            : [Config.DefaultSttLanguage])
            .Select(NormalizeLanguageCode)
            .ToList();

        var primaryLanguage = resolved[0];
        var encoding = ResolveSttEncoding(mimeType);

        logger.LogInformation("[STT] Recognizing | encoding={Encoding} languages=[{Langs}]",
            encoding, string.Join(", ", resolved));

        var client = await SpeechClient.CreateAsync(cancellationToken);

        var config = new RecognitionConfig
        {
            Encoding = encoding,
            LanguageCode = primaryLanguage,
            EnableAutomaticPunctuation = true
            // Model not specified — Google auto-selects based on encoding + language
        };

        // Add alternative languages for auto-detection (Google supports up to 3 alternatives)
        foreach (var alt in resolved.Skip(1).Take(3))
            config.AlternativeLanguageCodes.Add(alt);

        // Set sample rate only for encodings that require it
        if (encoding is RecognitionConfig.Types.AudioEncoding.WebmOpus
                     or RecognitionConfig.Types.AudioEncoding.OggOpus)
        {
            config.SampleRateHertz = 48000;
        }
        else if (encoding is RecognitionConfig.Types.AudioEncoding.Linear16
                          or RecognitionConfig.Types.AudioEncoding.Amr
                          or RecognitionConfig.Types.AudioEncoding.AmrWb)
        {
            config.SampleRateHertz = 16000;
        }

        var audio = RecognitionAudio.FromBytes(audioData);

        var response = await client.RecognizeAsync(config, audio, cancellationToken);

        if (response.Results.Count == 0 || response.Results[0].Alternatives.Count == 0)
        {
            logger.LogWarning("[STT] No speech recognized.");
            return new SttResult("", 0.0);
        }

        var firstResult = response.Results[0];
        var best = firstResult.Alternatives[0];
        var detectedLang = firstResult.LanguageCode; // populated when multiple languages were specified

        logger.LogInformation("[STT] Recognized: \"{Text}\" | lang={Lang} confidence={Conf:F2}",
            best.Transcript, detectedLang ?? primaryLanguage, best.Confidence);

        return new SttResult(best.Transcript, best.Confidence, detectedLang ?? (resolved.Count > 1 ? null : primaryLanguage));
    }

    public async Task StreamingRecognizeAsync(
        IAsyncEnumerable<byte[]> audioStream,
        string mimeType,
        Func<SttStreamingResult, Task> onResult,
        IReadOnlyList<string>? languageCodes = null,
        CancellationToken cancellationToken = default)
    {
        if (audioStream == null) return;

        await using var enumerator = audioStream.GetAsyncEnumerator(cancellationToken);
        
        // Bắt buộc phải có ít nhất 1 chunk audio thì mới khởi tạo kết nối Google Cloud để đỡ tốn tiền
        if (!await enumerator.MoveNextAsync())
        {
            logger.LogInformation("[STT Streaming] No audio chunks received. Aborting STT call.");
            return;
        }

        var resolved = (languageCodes is { Count: > 0 }
            ? languageCodes
            : [Config.DefaultSttLanguage])
            .Select(NormalizeLanguageCode)
            .ToList();

        var primaryLanguage = resolved[0];
        var encoding = ResolveSttEncoding(mimeType);

        logger.LogInformation("[STT Streaming] Starting | encoding={Encoding} languages=[{Langs}]",
            encoding, string.Join(", ", resolved));

        var client = await SpeechClient.CreateAsync(cancellationToken);

        var config = new RecognitionConfig
        {
            Encoding = encoding,
            LanguageCode = primaryLanguage,
            EnableAutomaticPunctuation = true
        };

        foreach (var alt in resolved.Skip(1).Take(3))
            config.AlternativeLanguageCodes.Add(alt);

        if (encoding is RecognitionConfig.Types.AudioEncoding.WebmOpus
                     or RecognitionConfig.Types.AudioEncoding.OggOpus)
        {
            config.SampleRateHertz = 48000; // Browsers usually record Opus at 48kHz
        }
        else if (encoding is RecognitionConfig.Types.AudioEncoding.Linear16
                          or RecognitionConfig.Types.AudioEncoding.Amr
                          or RecognitionConfig.Types.AudioEncoding.AmrWb)
        {
            config.SampleRateHertz = 16000;
        }

        using var streamingCall = client.StreamingRecognize();

        // Task to read responses from Google
        var responseTask = Task.Run(async () =>
        {
            try
            {
                var responseStream = streamingCall.GetResponseStream();
                while (await responseStream.MoveNextAsync(cancellationToken))
                {
                    var response = responseStream.Current;
                    if (response.Results.Count > 0)
                    {
                        var result = response.Results[0];
                        if (result.Alternatives.Count > 0)
                        {
                            var best = result.Alternatives[0];
                            await onResult(new SttStreamingResult(
                                best.Transcript,
                                result.IsFinal,
                                best.Confidence,
                                result.LanguageCode ?? primaryLanguage));
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "[STT Streaming] Error receiving from Google.");
            }
        }, cancellationToken);

        // Task to send audio from client to Google
        try
        {
            // Send config first
            await streamingCall.WriteAsync(new StreamingRecognizeRequest
            {
                StreamingConfig = new StreamingRecognitionConfig
                {
                    Config = config,
                    InterimResults = true // We want interim results for real-time
                }
            });

            // Send the first chunk we already read
            await streamingCall.WriteAsync(new StreamingRecognizeRequest
            {
                AudioContent = Google.Protobuf.ByteString.CopyFrom(enumerator.Current)
            });

            // Send the remaining chunks
            while (await enumerator.MoveNextAsync())
            {
                await streamingCall.WriteAsync(new StreamingRecognizeRequest
                {
                    AudioContent = Google.Protobuf.ByteString.CopyFrom(enumerator.Current)
                });
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "[STT Streaming] Error sending to Google.");
        }
        finally
        {
            try { await streamingCall.WriteCompleteAsync(); } catch { }
            await responseTask;
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Normalizes bare language codes to BCP-47 format required by Google APIs.
    /// e.g. "en" → "en-US", "vi" → "vi-VN"
    /// </summary>
    private static string NormalizeLanguageCode(string lang) => lang.ToLowerInvariant() switch
    {
        "en" => "en-US",
        "vi" => "vi-VN",
        "fr" => "fr-FR",
        "de" => "de-DE",
        "ja" => "ja-JP",
        "ko" => "ko-KR",
        "zh" => "zh-CN",
        _ => lang   // already fully qualified (e.g. "en-US") or unknown — pass through
    };

    private static RecognitionConfig.Types.AudioEncoding ResolveSttEncoding(string mimeType) =>
        mimeType.ToLowerInvariant() switch
        {
            "audio/webm" or "audio/webm;codecs=opus" or "audio/webm; codecs=opus"
                => RecognitionConfig.Types.AudioEncoding.WebmOpus,
            "audio/ogg" or "audio/ogg;codecs=opus" or "audio/ogg; codecs=opus"
                => RecognitionConfig.Types.AudioEncoding.OggOpus,
            "audio/wav" or "audio/x-wav" or "audio/wave"
                => RecognitionConfig.Types.AudioEncoding.Linear16,
            "audio/flac" or "audio/x-flac"
                => RecognitionConfig.Types.AudioEncoding.Flac,
            "audio/mp3" or "audio/mpeg"
                => RecognitionConfig.Types.AudioEncoding.Mp3,
            "audio/amr"
                => RecognitionConfig.Types.AudioEncoding.Amr,
            _ => RecognitionConfig.Types.AudioEncoding.EncodingUnspecified
        };
}
