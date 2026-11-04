using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Domain.VocabularyGames.Repositories;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Commands.SynthesizeFallingWordAudio;

public sealed class SynthesizeFallingWordAudioCommandHandler(
    IFallingWordGameRepository gameRepository,
    ICurrentUser currentUser,
    IPronunciationAudioSynthesizer audioSynthesizer)
    : ICommandHandler<SynthesizeFallingWordAudioCommand, FallingWordAudioDto>
{
    public async Task<FallingWordAudioDto> HandleAsync(SynthesizeFallingWordAudioCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(currentUser.UserId))
            throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        var session = await gameRepository.GetByIdWithRoundsForUserAsync(command.SessionId, currentUser.UserId, ct)
            ?? throw new NotFoundException($"Không tìm thấy phiên chơi với ID '{command.SessionId}'.");

        var round = session.Rounds.FirstOrDefault(r => r.Id == command.RoundId)
            ?? throw new NotFoundException($"Không tìm thấy lượt chơi với ID '{command.RoundId}'.");

        string text = round.TargetWordSnapshot.Trim();
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Từ vựng không có nội dung phát âm.");

        var req = command.Request;
        var audio = await audioSynthesizer.SynthesizeAsync(
            new PronunciationAudioSynthesisRequest(
                ApiKey: req.ApiKey,
                Text: text,
                TargetType: "word",
                TargetId: round.VocabularyId,
                VoiceName: req.VoiceName,
                SpeechSpeed: req.SpeechSpeed,
                Accent: req.Accent,
                ModelCode: req.ModelCode,
                MissingApiKeyMessage: "Vui lòng nhập Google AI Studio API Key ở menu hoặc hộp thoại để nghe phát âm mẫu.",
                IncompleteAudioMessage: "Chưa tạo được âm thanh hoàn chỉnh. Vui lòng thử lại.",
                ProviderTargetId: round.VocabularyId,
                VocabularyId: round.VocabularyId,
                SenseId: round.SenseId),
            ct);

        return new FallingWordAudioDto(
            audio.AudioBase64,
            audio.MimeType,
            audio.SampleRate,
            audio.DurationMs);
    }
}
