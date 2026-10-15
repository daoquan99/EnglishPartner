using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Domain.Pronunciation.GeneratedAudio;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Policies;

public sealed record ResolvedPronunciationTarget(
    Guid? TargetId,
    string TargetType,
    string Text,
    string TextHash,
    bool CanPersist
);

public sealed record ResolvedAssessmentTarget(
    Guid VocabularyId,
    Guid SenseId,
    string TargetType,
    Guid? TargetId,
    string ExpectedText
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

    Task<ResolvedAssessmentTarget> ResolveForAssessmentAsync(
        Guid vocabularyId,
        Guid senseId,
        string? targetType,
        Guid? targetId,
        string? fallbackReferenceText = null,
        CancellationToken ct = default);
}

public sealed class PronunciationTargetResolver : IPronunciationTargetResolver
{
    private readonly IAppReadDbContext _db;

    public PronunciationTargetResolver(IAppReadDbContext db)
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

                if (senseId.HasValue && senseId.Value != Guid.Empty)
                {
                    bool senseBelongs = await _db.VocabularySenses
                        .AsNoTracking()
                        .AnyAsync(s => s.Id == senseId.Value && s.VocabularyId == vocab.Id && !s.IsDeleted, ct);
                    if (!senseBelongs)
                    {
                        throw new InvalidOperationException("SenseId không thuộc từ vựng được chọn.");
                    }
                }

                string resolvedText = vocab.Word.Trim();
                string textHash = GeneratedPronunciationAudioEntity.ComputeTextHash(resolvedText);
                return new ResolvedPronunciationTarget(targetId.Value, "word", resolvedText, textHash, CanPersist: true);
            }
            else
            {
                var example = await _db.VocabularyExamples
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == targetId.Value && !e.IsDeleted, ct);

                if (example is null)
                {
                    throw new InvalidOperationException($"Không tìm thấy câu ví dụ hợp lệ với TargetId '{targetId.Value}'.");
                }

                var sense = await _db.VocabularySenses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == example.SenseId && !s.IsDeleted, ct);

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

    public async Task<ResolvedAssessmentTarget> ResolveForAssessmentAsync(
        Guid vocabularyId,
        Guid senseId,
        string? targetType,
        Guid? targetId,
        string? fallbackReferenceText = null,
        CancellationToken ct = default)
    {
        var vocab = await _db.Vocabularies
            .AsNoTracking()
            .Include(v => v.Senses).ThenInclude(s => s.Examples)
            .FirstOrDefaultAsync(v => v.Id == vocabularyId && !v.IsDeleted, ct);

        if (vocab is null)
        {
            throw new InvalidOperationException("Không tìm thấy từ vựng cần chấm phát âm.");
        }

        var activeSenses = vocab.Senses.Where(s => !s.IsDeleted).ToList();
        var sense = senseId != Guid.Empty
            ? activeSenses.FirstOrDefault(s => s.Id == senseId)
            : activeSenses.OrderByDescending(s => s.IsPrimary).ThenBy(s => s.DisplayOrder).FirstOrDefault();

        if (sense is null)
        {
            throw new InvalidOperationException("SenseId không thuộc từ vựng cần chấm phát âm.");
        }

        bool isSentence = string.Equals(targetType?.Trim(), "Sentence", StringComparison.OrdinalIgnoreCase);
        if (!isSentence)
        {
            if (targetId.HasValue && targetId.Value != Guid.Empty && targetId.Value != vocab.Id)
            {
                throw new InvalidOperationException("TargetId của từ vựng không khớp với VocabularyId.");
            }

            return new ResolvedAssessmentTarget(
                VocabularyId: vocab.Id,
                SenseId: sense.Id,
                TargetType: "Word",
                TargetId: targetId ?? vocab.Id,
                ExpectedText: vocab.Word.Trim());
        }

        var senseExamples = sense.Examples.Where(e => !e.IsDeleted).OrderBy(e => e.DisplayOrder).ToList();
        var allExamples = activeSenses.SelectMany(s => s.Examples.Where(e => !e.IsDeleted)).ToList();

        if (targetId.HasValue && targetId.Value != Guid.Empty)
        {
            var matchedEx = senseExamples.FirstOrDefault(e => e.Id == targetId.Value)
                ?? allExamples.FirstOrDefault(e => e.Id == targetId.Value);

            if (matchedEx is null)
            {
                throw new InvalidOperationException("TargetId câu ví dụ không thuộc từ vựng được chọn.");
            }

            return new ResolvedAssessmentTarget(
                VocabularyId: vocab.Id,
                SenseId: sense.Id,
                TargetType: "Sentence",
                TargetId: matchedEx.Id,
                ExpectedText: matchedEx.SentenceEn.Trim());
        }

        var firstExample = senseExamples.FirstOrDefault() ?? allExamples.FirstOrDefault();
        if (firstExample is not null)
        {
            return new ResolvedAssessmentTarget(
                VocabularyId: vocab.Id,
                SenseId: sense.Id,
                TargetType: "Sentence",
                TargetId: firstExample.Id,
                ExpectedText: firstExample.SentenceEn.Trim());
        }

        string expectedText = !string.IsNullOrWhiteSpace(fallbackReferenceText)
            ? fallbackReferenceText.Trim()
            : vocab.Word.Trim();

        return new ResolvedAssessmentTarget(
            VocabularyId: vocab.Id,
            SenseId: sense.Id,
            TargetType: "Sentence",
            TargetId: targetId,
            ExpectedText: expectedText);
    }
}
