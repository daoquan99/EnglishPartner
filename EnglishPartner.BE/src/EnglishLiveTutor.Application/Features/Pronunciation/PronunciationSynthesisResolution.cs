using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Pronunciation;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Pronunciation;

public sealed record ResolvedPronunciationTarget(
    Guid? TargetId,
    string TargetType,
    string Text,
    string TextHash,
    bool CanPersist
);

public static class PronunciationProfileNormalizer
{
    public static string NormalizeVoiceName(string? voiceName) =>
        string.IsNullOrWhiteSpace(voiceName) ? "Aoede" : voiceName.Trim();

    public static string NormalizeAccent(string? accent) =>
        string.Equals(accent?.Trim(), "UK", StringComparison.OrdinalIgnoreCase) ? "UK" : "US";

    public static decimal NormalizeSpeechSpeed(double? speechSpeed)
    {
        double speed = speechSpeed is null or <= 0 ? 1.0 : speechSpeed.Value;
        speed = Math.Clamp(speed, 0.5, 2.0);
        return Math.Round((decimal)speed, 2, MidpointRounding.AwayFromZero);
    }

    public static string NormalizeModelCode(string? modelCode)
    {
        string raw = (modelCode ?? "gemini-3.8-live").Trim().ToLowerInvariant();
        return raw.Contains("thinking")
            ? "gemini-3.8-live-extended-thinking"
            : "gemini-3.8-live";
    }

    public static string NormalizeTargetType(string? targetType)
    {
        string raw = targetType?.Trim().ToLowerInvariant() ?? "word";
        return raw == "sentence" ? "sentence" : "word";
    }
}

public interface IPronunciationTargetResolver
{
    Task<ResolvedPronunciationTarget> ResolveAsync(
        Guid? targetId,
        string? targetType,
        string? text,
        Guid? vocabularyId = null,
        Guid? senseId = null,
        CancellationToken ct = default);
}

public sealed class PronunciationTargetResolver : IPronunciationTargetResolver
{
    private readonly IAppDbContext _db;

    public PronunciationTargetResolver(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<ResolvedPronunciationTarget> ResolveAsync(
        Guid? targetId,
        string? targetType,
        string? text,
        Guid? vocabularyId = null,
        Guid? senseId = null,
        CancellationToken ct = default)
    {
        if (targetId.HasValue && targetId.Value != Guid.Empty)
        {
            string normalizedType = targetType?.Trim().ToLowerInvariant() ?? string.Empty;
            if (normalizedType is not ("word" or "sentence"))
            {
                throw new InvalidOperationException($"TargetType không hợp lệ: '{targetType}'. Chỉ chấp nhận 'word' hoặc 'sentence'.");
            }

            if (normalizedType == "word")
            {
                if (vocabularyId.HasValue && vocabularyId.Value != Guid.Empty && vocabularyId.Value != targetId.Value)
                {
                    throw new InvalidOperationException("VocabularyId không khớp với TargetId của từ vựng.");
                }

                var vocab = await _db.Vocabularies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == targetId.Value && !v.IsDeleted, ct);

                if (vocab is null)
                {
                    throw new InvalidOperationException($"Không tìm thấy từ vựng hợp lệ với TargetId '{targetId.Value}'.");
                }

                string resolvedText = vocab.Word.Trim();
                string textHash = GeneratedPronunciationAudioEntity.ComputeTextHash(resolvedText);
                return new ResolvedPronunciationTarget(targetId.Value, "word", resolvedText, textHash, CanPersist: true);
            }
            else
            {
                // sentence
                var example = await _db.VocabularyExamples
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == targetId.Value, ct);

                if (example is null)
                {
                    throw new InvalidOperationException($"Không tìm thấy câu ví dụ hợp lệ với TargetId '{targetId.Value}'.");
                }

                var sense = await _db.VocabularySenses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == example.SenseId, ct);

                if (sense is null)
                {
                    throw new InvalidOperationException($"Không tìm thấy Sense của câu ví dụ '{targetId.Value}'.");
                }

                if (senseId.HasValue && senseId.Value != Guid.Empty && senseId.Value != sense.Id)
                {
                    throw new InvalidOperationException("SenseId không khớp với ngữ cảnh của câu ví dụ.");
                }

                var vocab = await _db.Vocabularies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == sense.VocabularyId && !v.IsDeleted, ct);

                if (vocab is null)
                {
                    throw new InvalidOperationException($"Không tìm thấy từ vựng hoặc từ vựng đã bị xóa cho câu ví dụ '{targetId.Value}'.");
                }

                if (vocabularyId.HasValue && vocabularyId.Value != Guid.Empty && vocabularyId.Value != vocab.Id)
                {
                    throw new InvalidOperationException("VocabularyId không khớp với ngữ cảnh của câu ví dụ.");
                }

                string resolvedText = example.SentenceEn.Trim();
                string textHash = GeneratedPronunciationAudioEntity.ComputeTextHash(resolvedText);
                return new ResolvedPronunciationTarget(targetId.Value, "sentence", resolvedText, textHash, CanPersist: true);
            }
        }

        // Legacy text-only path
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Yêu cầu phát âm phải có TargetId hoặc Text hợp lệ.");
        }

        string legacyType = PronunciationProfileNormalizer.NormalizeTargetType(targetType);
        string legacyText = text.Trim();
        string legacyHash = GeneratedPronunciationAudioEntity.ComputeTextHash(legacyText);

        return new ResolvedPronunciationTarget(
            TargetId: null,
            TargetType: legacyType,
            Text: legacyText,
            TextHash: legacyHash,
            CanPersist: false
        );
    }
}
