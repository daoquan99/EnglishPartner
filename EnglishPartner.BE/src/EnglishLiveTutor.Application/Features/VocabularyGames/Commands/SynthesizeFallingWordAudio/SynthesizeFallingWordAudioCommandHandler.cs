using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Lessons.Commands.SynthesizeExerciseAudio;
using EnglishLiveTutor.Application.Features.Pronunciation;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;
using EnglishLiveTutor.Domain.VocabularyGames.Repositories;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Commands.SynthesizeFallingWordAudio;

public sealed class SynthesizeFallingWordAudioCommandHandler(
    IFallingWordGameRepository gameRepository,
    ICurrentUser currentUser,
    ILivePronunciationTtsSynthesizer synthesizer,
    IGeneratedPronunciationAudioRepository audioRepository,
    IPronunciationSynthesisLock synthesisLock)
    : ICommandHandler<SynthesizeFallingWordAudioCommand, ExerciseAudioDto>
{
    public async Task<ExerciseAudioDto> HandleAsync(SynthesizeFallingWordAudioCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(currentUser.UserId))
            throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        var session = await gameRepository.GetByIdWithRoundsAsync(command.SessionId, ct)
            ?? throw new NotFoundException($"Không tìm thấy phiên chơi với ID '{command.SessionId}'.");

        if (session.UserId != currentUser.UserId)
            throw new ForbiddenException("Bạn không có quyền truy cập phiên chơi này.");

        var round = session.Rounds.FirstOrDefault(r => r.Id == command.RoundId)
            ?? throw new NotFoundException($"Không tìm thấy lượt chơi với ID '{command.RoundId}'.");

        string text = round.TargetWordSnapshot.Trim();
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Từ vựng không có nội dung phát âm.");

        var req = command.Request;
        string voice = PronunciationProfileNormalizer.NormalizeVoiceName(req.VoiceName);
        decimal speed = PronunciationProfileNormalizer.NormalizeSpeechSpeed(req.SpeechSpeed);
        string accent = PronunciationProfileNormalizer.NormalizeAccent(req.Accent);
        string model = PronunciationProfileNormalizer.NormalizeModelCode(req.ModelCode);
        string hash = GeneratedPronunciationAudioEntity.ComputeTextHash(text);
        const string targetType = "word";

        // Check persistent audio cache using vocabularyId first (shares audio with Vocabulary Catalog / Daily Learning)
        var cached = await audioRepository.GetValidAudioAsync(targetType, round.VocabularyId, voice, speed, accent, model, hash, ct);
        if (cached is not null)
        {
            return new ExerciseAudioDto(Convert.ToBase64String(cached.AudioData), cached.MimeType, cached.SampleRate, cached.DurationMs);
        }

        if (string.IsNullOrWhiteSpace(req.ApiKey))
        {
            throw new InvalidOperationException("Vui lòng nhập Google AI Studio API Key ở menu hoặc hộp thoại để nghe phát âm mẫu.");
        }

        using var lease = await synthesisLock.AcquireLockAsync($"{targetType}:{round.VocabularyId}:{voice}:{speed}:{accent}:{model}:{hash}", ct);
        cached = await audioRepository.GetValidAudioAsync(targetType, round.VocabularyId, voice, speed, accent, model, hash, ct);
        if (cached is not null)
        {
            return new ExerciseAudioDto(Convert.ToBase64String(cached.AudioData), cached.MimeType, cached.SampleRate, cached.DurationMs);
        }

        var audio = await synthesizer.SynthesizeSpeechAsync(new SynthesizePronunciationAudioRequest(
            ApiKey: req.ApiKey.Trim(),
            Text: text,
            TargetType: "word",
            Accent: accent,
            VoiceName: voice,
            SpeechSpeed: (double)speed,
            ModelCode: model,
            TargetId: round.VocabularyId,
            VocabularyId: round.VocabularyId,
            SenseId: round.SenseId), ct);

        if (!audio.IsComplete || string.IsNullOrWhiteSpace(audio.AudioBase64))
        {
            throw new InvalidOperationException("Chưa tạo được âm thanh hoàn chỉnh. Vui lòng thử lại.");
        }

        var bytes = Convert.FromBase64String(audio.AudioBase64);
        var saved = new GeneratedPronunciationAudioEntity(
            targetType,
            round.VocabularyId,
            voice,
            speed,
            accent,
            model,
            text,
            hash,
            bytes,
            audio.MimeType,
            audio.SampleRate,
            audio.DurationMs,
            audio.SystemPromptUsed);

        await audioRepository.AddAsync(saved, ct);
        await audioRepository.SaveChangesAsync(ct);

        return new ExerciseAudioDto(audio.AudioBase64, audio.MimeType, audio.SampleRate, audio.DurationMs);
    }
}
