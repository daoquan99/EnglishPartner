using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Pronunciation;
using EnglishLiveTutor.Domain.Lessons.Enums;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Lessons.Commands.SynthesizeExerciseAudio;

public sealed record SynthesizeExerciseAudioRequest(
    string? ApiKey = null,
    string VoiceName = "Aoede",
    double SpeechSpeed = 1.0,
    string Accent = "US",
    string ModelCode = "gemini-3.8-live",
    string? CardId = null);

// Do not return Text/SystemPromptUsed: spelling answers and dictation transcripts stay on the server.
public sealed record ExerciseAudioDto(string AudioBase64, string MimeType, int SampleRate, int DurationMs);

public sealed record SynthesizeExerciseAudioCommand(Guid AttemptId, Guid ExerciseId, SynthesizeExerciseAudioRequest Request)
    : ICommand<ExerciseAudioDto>;

public sealed class SynthesizeExerciseAudioCommandHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    ILivePronunciationTtsSynthesizer synthesizer,
    IGeneratedPronunciationAudioRepository audioRepository,
    IPronunciationSynthesisLock synthesisLock)
    : ICommandHandler<SynthesizeExerciseAudioCommand, ExerciseAudioDto>
{
    public async Task<ExerciseAudioDto> HandleAsync(SynthesizeExerciseAudioCommand command, CancellationToken ct = default)
    {
        var attempt = await db.LessonAttempts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == command.AttemptId, ct)
            ?? throw new NotFoundException("Không tìm thấy lượt làm bài.");
        if (string.IsNullOrWhiteSpace(currentUser.UserId) || attempt.UserId != currentUser.UserId)
            throw new ForbiddenException("Bạn không có quyền truy cập âm thanh của lượt làm bài này.");

        var lesson = await db.Lessons.AsNoTracking().Include(l => l.Exercises)
            .FirstOrDefaultAsync(l => l.Id == attempt.LessonId, ct)
            ?? throw new NotFoundException("Không tìm thấy bài học.");
        var exercise = lesson.Exercises.FirstOrDefault(e => e.Id == command.ExerciseId)
            ?? throw new NotFoundException("Không tìm thấy bài tập trong lượt làm bài.");

        var content = JsonNode.Parse(exercise.ContentJson);
        var grading = JsonNode.Parse(exercise.GradingDefinitionJson ?? "{}");
        string? text = exercise.Type switch
        {
            ExerciseType.SpellingPractice => Read(grading, "targetWord") ?? Read(content, "audioText", "word"),
            ExerciseType.Dictation => Read(grading, "transcript", "referenceTranscript") ?? Read(content, "transcript", "audioText"),
            ExerciseType.ListeningComprehension => Read(content, "transcript", "audioText") ?? Read(grading, "transcript", "referenceTranscript"),
            ExerciseType.Shadowing => Read(content, "referenceText", "text", "audioText", "transcript") ?? Read(grading, "referenceTranscript", "transcript", "referenceText"),
            ExerciseType.SpeakingPractice => string.Join(". ", new[] { Read(content, "topic", "prompt"), Read(content, "instructions", "taskInstructions") }.Where(s => !string.IsNullOrWhiteSpace(s))),
            ExerciseType.Flashcards => ResolveCard(content, command.Request.CardId),
            _ => throw new InvalidOperationException("Loại bài tập này không hỗ trợ âm thanh mẫu.")
        };
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Bài tập chưa có nội dung để tạo âm thanh mẫu. Vui lòng bổ sung nội dung trong bài học.");
        // Match the existing GeneratedPronunciationAudios.TextSnapshot column without a schema change.
        if (text.Length > 2000)
            throw new InvalidOperationException("Nội dung đọc mẫu quá dài (tối đa 2000 ký tự).");

        var req = command.Request;
        string voice = PronunciationProfileNormalizer.NormalizeVoiceName(req.VoiceName);
        decimal speed = PronunciationProfileNormalizer.NormalizeSpeechSpeed(req.SpeechSpeed);
        string accent = PronunciationProfileNormalizer.NormalizeAccent(req.Accent);
        string model = PronunciationProfileNormalizer.NormalizeModelCode(req.ModelCode);
        string hash = GeneratedPronunciationAudioEntity.ComputeTextHash(text);
        const string targetType = "lesson-exercise";
        var cached = await audioRepository.GetValidAudioAsync(targetType, exercise.Id, voice, speed, accent, model, hash, ct);
        if (cached is not null) return ToDto(cached);
        if (string.IsNullOrWhiteSpace(req.ApiKey))
            throw new InvalidOperationException("Vui lòng nhập Google AI Studio API Key ở menu bên trái để tạo âm thanh mẫu.");

        using var lease = await synthesisLock.AcquireLockAsync($"{targetType}:{exercise.Id}:{voice}:{speed}:{accent}:{model}:{hash}", ct);
        cached = await audioRepository.GetValidAudioAsync(targetType, exercise.Id, voice, speed, accent, model, hash, ct);
        if (cached is not null) return ToDto(cached);

        var audio = await synthesizer.SynthesizeSpeechAsync(new SynthesizePronunciationAudioRequest(
            ApiKey: req.ApiKey.Trim(), Text: text,
            TargetType: exercise.Type is ExerciseType.SpellingPractice or ExerciseType.Flashcards ? "word" : "sentence",
            Accent: accent, VoiceName: voice, SpeechSpeed: (double)speed, ModelCode: model), ct);
        if (!audio.IsComplete || string.IsNullOrWhiteSpace(audio.AudioBase64))
            throw new InvalidOperationException("Chưa tạo được âm thanh hoàn chỉnh. Vui lòng thử lại.");
        var bytes = Convert.FromBase64String(audio.AudioBase64);
        var saved = new GeneratedPronunciationAudioEntity(targetType, exercise.Id, voice, speed, accent, model,
            text, hash, bytes, audio.MimeType, audio.SampleRate, audio.DurationMs, audio.SystemPromptUsed);
        await audioRepository.AddAsync(saved, ct);
        await audioRepository.SaveChangesAsync(ct);
        return ToDto(saved);
    }

    private static ExerciseAudioDto ToDto(GeneratedPronunciationAudioEntity audio) =>
        new(Convert.ToBase64String(audio.AudioData), audio.MimeType, audio.SampleRate, audio.DurationMs);

    private static string? Read(JsonNode? node, params string[] names) => names
        .Select(name => node?[name]?.GetValue<string>()).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private static string? ResolveCard(JsonNode? content, string? cardId)
    {
        var cards = content?["cards"]?.AsArray();
        var card = cards?.FirstOrDefault(c => c?["id"]?.ToString() == cardId);
        if (card is null) throw new InvalidOperationException("Vui lòng chọn đúng thẻ từ cần nghe.");
        return Read(card, "term", "word");
    }
}
