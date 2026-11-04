using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Learning;

public sealed class UserLearningSettingsEntity : AggregateRoot<Guid>
{
    public static readonly string[] AllowedCefrLevels = ["A1", "A2", "B1", "B2", "C1", "C2"];

    public string UserId { get; private set; } = string.Empty;
    public int DailyVocabularyTarget { get; private set; } = 20;
    public string CefrLevel { get; private set; } = "B1";
    public byte[]? RowVersion { get; private set; }

    private UserLearningSettingsEntity()
    {
    }

    public UserLearningSettingsEntity(
        string userId,
        int dailyVocabularyTarget = 20,
        string cefrLevel = "B1",
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId không được để trống.", nameof(userId));

        UserId = userId.Trim();
        SetTarget(dailyVocabularyTarget);
        SetLevel(cefrLevel);
    }

    public static UserLearningSettingsEntity Create(
        string userId,
        int dailyVocabularyTarget = 20,
        string cefrLevel = "B1",
        Guid? id = null) =>
        new(userId, dailyVocabularyTarget, cefrLevel, id);

    public void Update(int dailyVocabularyTarget, string cefrLevel)
    {
        SetTarget(dailyVocabularyTarget);
        SetLevel(cefrLevel);
    }

    public static bool IsValidCefrLevel(string? level) =>
        !string.IsNullOrWhiteSpace(level) &&
        AllowedCefrLevels.Contains(level.Trim().ToUpperInvariant());

    public static string NormalizeCefrLevel(string? level, string fallback = "B1")
    {
        if (string.IsNullOrWhiteSpace(level)) return fallback;
        string normalized = level.Trim().ToUpperInvariant();
        return AllowedCefrLevels.Contains(normalized) ? normalized : fallback;
    }

    private void SetTarget(int target)
    {
        if (target < 1 || target > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(target),
                target,
                "Mục tiêu học từ vựng mỗi ngày phải nằm trong khoảng từ 1 đến 100 từ.");
        }
        DailyVocabularyTarget = target;
    }

    private void SetLevel(string level)
    {
        if (string.IsNullOrWhiteSpace(level))
        {
            throw new ArgumentException("CEFR Level không được để trống.", nameof(level));
        }

        string normalized = level.Trim().ToUpperInvariant();
        if (!AllowedCefrLevels.Contains(normalized))
        {
            throw new ArgumentException(
                $"CEFR Level không hợp lệ: '{level}'. Chỉ chấp nhận A1, A2, B1, B2, C1 hoặc C2.",
                nameof(level));
        }

        CefrLevel = normalized;
    }
}
