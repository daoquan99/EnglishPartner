using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Commands.SynthesizePronunciationAudio;

public sealed record SynthesizePronunciationAudioCommand(SynthesizePronunciationAudioRequest Request)
    : ICommand<SynthesizePronunciationAudioResponseDto>;

public sealed class SynthesizePronunciationAudioCommandHandler : ICommandHandler<SynthesizePronunciationAudioCommand, SynthesizePronunciationAudioResponseDto>
{
    private readonly ILivePronunciationTtsSynthesizer _synthesizer;
    private readonly IPronunciationTargetResolver _targetResolver;
    private readonly IGeneratedPronunciationAudioRepository _audioRepo;
    private readonly IPronunciationSynthesisLock _synthesisLock;

    public SynthesizePronunciationAudioCommandHandler(
        ILivePronunciationTtsSynthesizer synthesizer,
        IPronunciationTargetResolver targetResolver,
        IGeneratedPronunciationAudioRepository audioRepo,
        IPronunciationSynthesisLock synthesisLock)
    {
        _synthesizer = synthesizer;
        _targetResolver = targetResolver;
        _audioRepo = audioRepo;
        _synthesisLock = synthesisLock;
    }

    public async Task<SynthesizePronunciationAudioResponseDto> HandleAsync(
        SynthesizePronunciationAudioCommand command,
        CancellationToken ct = default)
    {
        var req = command.Request;
        var resolved = await _targetResolver.ResolveAsync(
            targetId: req.TargetId,
            targetType: req.TargetType,
            text: req.Text,
            vocabularyId: req.VocabularyId,
            senseId: req.SenseId,
            ct: ct);

        string voiceName = PronunciationProfileNormalizer.NormalizeVoiceName(req.VoiceName);
        decimal speechSpeed = PronunciationProfileNormalizer.NormalizeSpeechSpeed(req.SpeechSpeed);
        string accent = PronunciationProfileNormalizer.NormalizeAccent(req.Accent);
        string modelCode = PronunciationProfileNormalizer.NormalizeModelCode(req.ModelCode);

        if (resolved.CanPersist)
        {
            var cached = await _audioRepo.GetValidAudioAsync(
                targetType: resolved.TargetType,
                targetId: resolved.TargetId!.Value,
                voiceName: voiceName,
                speechSpeed: speechSpeed,
                accent: accent,
                modelCode: modelCode,
                textHash: resolved.TextHash,
                ct: ct);

            if (cached is not null)
            {
                return ToResponseDto(cached);
            }

            if (string.IsNullOrWhiteSpace(req.ApiKey))
            {
                throw new InvalidOperationException("Vui lòng nhập Google AI Studio API Key để tạo âm thanh phát âm.");
            }

            string lockKey = $"{resolved.TargetType}:{resolved.TargetId.Value}:{voiceName}:{speechSpeed:0.00}:{accent}:{modelCode}:{resolved.TextHash}";
            using (await _synthesisLock.AcquireLockAsync(lockKey, ct))
            {
                cached = await _audioRepo.GetValidAudioAsync(
                    targetType: resolved.TargetType,
                    targetId: resolved.TargetId.Value,
                    voiceName: voiceName,
                    speechSpeed: speechSpeed,
                    accent: accent,
                    modelCode: modelCode,
                    textHash: resolved.TextHash,
                    ct: ct);

                if (cached is not null)
                {
                    return ToResponseDto(cached);
                }

                var providerReq = req with
                {
                    ApiKey = req.ApiKey.Trim(),
                    Text = resolved.Text,
                    TargetType = resolved.TargetType,
                    Accent = accent,
                    VoiceName = voiceName,
                    SpeechSpeed = (double)speechSpeed,
                    ModelCode = modelCode,
                    TargetId = resolved.TargetId,
                    VocabularyId = req.VocabularyId,
                    SenseId = req.SenseId
                };

                var response = await _synthesizer.SynthesizeSpeechAsync(providerReq, ct);

                if (response.IsComplete && !string.IsNullOrWhiteSpace(response.AudioBase64))
                {
                    byte[] wavBytes = Convert.FromBase64String(response.AudioBase64);
                    if (wavBytes.Length > 0)
                    {
                        var audioEntity = new GeneratedPronunciationAudioEntity(
                            targetType: resolved.TargetType,
                            targetId: resolved.TargetId.Value,
                            voiceName: voiceName,
                            speechSpeed: speechSpeed,
                            accent: accent,
                            modelCode: modelCode,
                            textSnapshot: resolved.Text,
                            textHash: resolved.TextHash,
                            audioData: wavBytes,
                            mimeType: response.MimeType,
                            sampleRate: response.SampleRate,
                            durationMs: response.DurationMs,
                            systemPromptUsed: response.SystemPromptUsed);

                        try
                        {
                            await _audioRepo.AddAsync(audioEntity, ct);
                            await _audioRepo.SaveChangesAsync(ct);
                        }
                        catch (DbUpdateException)
                        {
                            // Ignore unique constraint or concurrent insert conflicts across multiple instances
                        }
                    }
                }

                return response;
            }
        }

        // Legacy text-only path (no persistent cache)
        if (string.IsNullOrWhiteSpace(req.ApiKey))
        {
            throw new InvalidOperationException("Vui lòng nhập Google AI Studio API Key để tạo âm thanh phát âm.");
        }

        var legacyReq = req with
        {
            ApiKey = req.ApiKey.Trim(),
            Text = resolved.Text,
            TargetType = resolved.TargetType,
            Accent = accent,
            VoiceName = voiceName,
            SpeechSpeed = (double)speechSpeed,
            ModelCode = modelCode
        };

        return await _synthesizer.SynthesizeSpeechAsync(legacyReq, ct);
    }

    private static SynthesizePronunciationAudioResponseDto ToResponseDto(GeneratedPronunciationAudioEntity entity)
    {
        return new SynthesizePronunciationAudioResponseDto(
            Text: entity.TextSnapshot,
            TargetType: entity.TargetType,
            Accent: entity.Accent,
            VoiceName: entity.VoiceName,
            SpeechSpeed: (double)entity.SpeechSpeed,
            ModelCodeUsed: entity.ModelCode,
            SystemPromptUsed: entity.SystemPromptUsed,
            MimeType: entity.MimeType,
            SampleRate: entity.SampleRate,
            DurationMs: entity.DurationMs,
            AudioBase64: Convert.ToBase64String(entity.AudioData),
            IsComplete: true
        );
    }
}
