namespace EnglishLiveTutor.Domain.Common.ValueObjects;

/// <summary>
/// Value object representing a spaced-repetition review rating (Again, Hard, Good, Easy).
/// Rejects unrecognized non-empty rating strings instead of silently misclassifying them as Good.
/// </summary>
public readonly record struct ReviewRating
{
    public const string Again = "Again";
    public const string Hard = "Hard";
    public const string Good = "Good";
    public const string Easy = "Easy";

    public string Value { get; }

    public ReviewRating(string? raw)
    {
        Value = Parse(raw);
    }

    public static string Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Good;
        }

        return raw.Trim().ToLowerInvariant() switch
        {
            "again" => Again,
            "hard" => Hard,
            "good" => Good,
            "easy" => Easy,
            _ => throw new InvalidOperationException(
                $"Đánh giá ôn tập '{raw.Trim()}' không hợp lệ. Chỉ chấp nhận Again, Hard, Good hoặc Easy.")
        };
    }

    public override string ToString() => Value;

    public static implicit operator string(ReviewRating rating) => rating.Value;
}
