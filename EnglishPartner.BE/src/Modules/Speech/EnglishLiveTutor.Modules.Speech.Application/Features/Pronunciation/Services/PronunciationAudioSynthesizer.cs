using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Services;

public sealed class PronunciationAudioSynthesizer(
    ILivePronunciationTtsSynthesizer provider,
    IGeneratedPronunciationAudioRepository audioRepository,
    IPronunciationSynthesisLock synthesisLock)
    : IPronunciationAudioSynthesizer
{
    public async Task<PronunciationAudioSynthesisResult> SynthesizeAsync(
        PronunciationAudioSynthesisRequest request,
        CancellationToken ct = default)
    {
        string targetType = request.TargetType;
        string voice = PronunciationProfileNormalizer.NormalizeVoiceName(request.VoiceName);
        decimal speed = PronunciationProfileNormalizer.NormalizeSpeechSpeed(request.SpeechSpeed);
        string accent = PronunciationProfileNormalizer.NormalizeAccent(request.Accent);
        string model = PronunciationProfileNormalizer.NormalizeModelCode(request.ModelCode);
        string hash = GeneratedPronunciationAudioEntity.ComputeTextHash(request.Text);

        var cached = await audioRepository.GetValidAudioAsync(
            targetType, request.TargetId, voice, speed, accent, model, hash, ct);
        if (cached is not null)
            return ToResult(cached);

        if (string.IsNullOrWhiteSpace(request.ApiKey))
            throw new InvalidOperationException(request.MissingApiKeyMessage);

        using var lease = await synthesisLock.AcquireLockAsync(
            $"{targetType}:{request.TargetId}:{voice}:{speed}:{accent}:{model}:{hash}", ct);

        cached = await audioRepository.GetValidAudioAsync(
            targetType, request.TargetId, voice, speed, accent, model, hash, ct);
        if (cached is not null)
            return ToResult(cached);

        var audio = await provider.SynthesizeSpeechAsync(
            new SynthesizePronunciationAudioRequest(
                ApiKey: request.ApiKey.Trim(),
                Text: request.Text,
                TargetType: PronunciationProfileNormalizer.NormalizeTargetType(
                    request.ProviderTargetType ?? request.TargetType),
                Accent: accent,
                VoiceName: voice,
                SpeechSpeed: (double)speed,
                ModelCode: model,
                TargetId: request.ProviderTargetId,
                VocabularyId: request.VocabularyId,
                SenseId: request.SenseId),
            ct);

        if (!audio.IsComplete || string.IsNullOrWhiteSpace(audio.AudioBase64))
            throw new InvalidOperationException(request.IncompleteAudioMessage);

        var bytes = Convert.FromBase64String(audio.AudioBase64);
        var saved = new GeneratedPronunciationAudioEntity(
            targetType,
            request.TargetId,
            voice,
            speed,
            accent,
            model,
            request.Text,
            hash,
            bytes,
            audio.MimeType,
            audio.SampleRate,
            audio.DurationMs,
            audio.SystemPromptUsed);

        await audioRepository.AddAsync(saved, ct);
        await audioRepository.SaveChangesAsync(ct);

        return new PronunciationAudioSynthesisResult(
            AudioBase64: request.ReturnStoredAudioBytesOnSynthesis
                ? Convert.ToBase64String(bytes)
                : audio.AudioBase64,
            MimeType: audio.MimeType,
            SampleRate: audio.SampleRate,
            DurationMs: audio.DurationMs);
    }

    private static PronunciationAudioSynthesisResult ToResult(GeneratedPronunciationAudioEntity audio) =>
        new(
            AudioBase64: Convert.ToBase64String(audio.AudioData),
            MimeType: audio.MimeType,
            SampleRate: audio.SampleRate,
            DurationMs: audio.DurationMs);
}
