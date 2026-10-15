using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetPronunciationAudio;

public sealed record GetPronunciationAudioQuery(
    string TargetType,
    Guid TargetId,
    string? VoiceName = null,
    double? SpeechSpeed = null,
    string? Accent = null,
    string? ModelCode = null,
    Guid? VocabularyId = null,
    Guid? SenseId = null
) : IQuery<SynthesizePronunciationAudioResponseDto?>;

public sealed class GetPronunciationAudioQueryHandler : IQueryHandler<GetPronunciationAudioQuery, SynthesizePronunciationAudioResponseDto?>
{
    private readonly IPronunciationTargetResolver _targetResolver;
    private readonly IGeneratedPronunciationAudioRepository _audioRepo;

    public GetPronunciationAudioQueryHandler(
        IPronunciationTargetResolver targetResolver,
        IGeneratedPronunciationAudioRepository audioRepo)
    {
        _targetResolver = targetResolver;
        _audioRepo = audioRepo;
    }

    public async Task<SynthesizePronunciationAudioResponseDto?> HandleAsync(
        GetPronunciationAudioQuery query,
        CancellationToken ct = default)
    {
        var resolved = await _targetResolver.ResolveAsync(
            targetId: query.TargetId,
            targetType: query.TargetType,
            text: null,
            vocabularyId: query.VocabularyId,
            senseId: query.SenseId,
            ct: ct);

        string voiceName = PronunciationProfileNormalizer.NormalizeVoiceName(query.VoiceName);
        decimal speechSpeed = PronunciationProfileNormalizer.NormalizeSpeechSpeed(query.SpeechSpeed);
        string accent = PronunciationProfileNormalizer.NormalizeAccent(query.Accent);
        string modelCode = PronunciationProfileNormalizer.NormalizeModelCode(query.ModelCode);

        var cached = await _audioRepo.GetValidAudioAsync(
            targetType: resolved.TargetType,
            targetId: resolved.TargetId!.Value,
            voiceName: voiceName,
            speechSpeed: speechSpeed,
            accent: accent,
            modelCode: modelCode,
            textHash: resolved.TextHash,
            ct: ct);

        if (cached is null)
        {
            return null;
        }

        return new SynthesizePronunciationAudioResponseDto(
            Text: cached.TextSnapshot,
            TargetType: cached.TargetType,
            Accent: cached.Accent,
            VoiceName: cached.VoiceName,
            SpeechSpeed: (double)cached.SpeechSpeed,
            ModelCodeUsed: cached.ModelCode,
            SystemPromptUsed: cached.SystemPromptUsed,
            MimeType: cached.MimeType,
            SampleRate: cached.SampleRate,
            DurationMs: cached.DurationMs,
            AudioBase64: Convert.ToBase64String(cached.AudioData),
            IsComplete: true
        );
    }
}
