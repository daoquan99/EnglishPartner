using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Application.Modules.Lessons;
using EnglishLiveTutor.Domain.Common.Exercises;
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
    ILessonsDbContext db,
    ICurrentUser currentUser,
    IPronunciationAudioSynthesizer audioSynthesizer)
    : ICommandHandler<SynthesizeExerciseAudioCommand, ExerciseAudioDto>
{
    public async Task<ExerciseAudioDto> HandleAsync(SynthesizeExerciseAudioCommand command, CancellationToken ct = default)
    {
        string? currentUserId = currentUser.UserId;
        if (string.IsNullOrWhiteSpace(currentUserId))
            throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        var attempt = await db.LessonAttempts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == command.AttemptId && a.UserId == currentUserId, ct)
            ?? throw new NotFoundException("Không tìm thấy lượt làm bài.");

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
        var audio = await audioSynthesizer.SynthesizeAsync(
            new PronunciationAudioSynthesisRequest(
                ApiKey: req.ApiKey,
                Text: text,
                TargetType: "lesson-exercise",
                ProviderTargetType: exercise.Type is ExerciseType.SpellingPractice or ExerciseType.Flashcards ? "word" : "sentence",
                TargetId: exercise.Id,
                VoiceName: req.VoiceName,
                SpeechSpeed: req.SpeechSpeed,
                Accent: req.Accent,
                ModelCode: req.ModelCode,
                MissingApiKeyMessage: "Vui lòng nhập Google AI Studio API Key ở menu bên trái để tạo âm thanh mẫu.",
                IncompleteAudioMessage: "Chưa tạo được âm thanh hoàn chỉnh. Vui lòng thử lại.",
                ReturnStoredAudioBytesOnSynthesis: true),
            ct);

        return new ExerciseAudioDto(audio.AudioBase64, audio.MimeType, audio.SampleRate, audio.DurationMs);
    }

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
