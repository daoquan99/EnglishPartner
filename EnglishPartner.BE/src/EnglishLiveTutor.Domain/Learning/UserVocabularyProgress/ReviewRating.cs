namespace EnglishLiveTutor.Domain.Learning.UserVocabularyProgress;

public static class ReviewRating
{
    public const string Again = "Again";
    public const string Hard = "Hard";
    public const string Good = "Good";
    public const string Easy = "Easy";

    public static string NormalizeOrThrow(string? ratingRaw)
    {
        if (string.IsNullOrWhiteSpace(ratingRaw))
        {
            return Good;
        }

        return ratingRaw.Trim().ToLowerInvariant() switch
        {
            "again" => Again,
            "hard" => Hard,
            "good" => Good,
            "easy" => Easy,
            _ => throw new InvalidOperationException($"Mức đánh giá ôn tập '{ratingRaw}' không hợp lệ. Chỉ chấp nhận: Again, Hard, Good, Easy.")
        };
    }
}
