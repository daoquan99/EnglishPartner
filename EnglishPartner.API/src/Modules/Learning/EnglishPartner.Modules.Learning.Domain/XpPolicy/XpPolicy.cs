namespace EnglishPartner.Modules.Learning.Domain.XpPolicy;

/// <summary>
/// Central XP calculation rules for the Learning module.
/// All XP logic must go through this class — never inline in handlers.
/// </summary>
public static class XpPolicy
{
    /// <summary>Base XP earned per item in a completed study session.</summary>
    public const int XpPerSessionItem = 5;

    /// <summary>Bonus XP awarded when session accuracy is >= 80%.</summary>
    public const int HighScoreBonusXp = 10;

    /// <summary>Score percentage threshold (inclusive) to earn the high-score bonus.</summary>
    public const double HighScoreThreshold = 0.8;

    /// <summary>
    /// Calculates total XP earned from completing a study session.
    /// Formula: (totalItems * 5) + 10 if score >= 80%.
    /// </summary>
    public static int FromStudySession(int totalItems, int score)
    {
        if (totalItems <= 0) return 0;

        var baseXp    = totalItems * XpPerSessionItem;
        var accuracy  = (double)score / totalItems;
        var bonusXp   = accuracy >= HighScoreThreshold ? HighScoreBonusXp : 0;

        return baseXp + bonusXp;
    }

    /// <summary>XP earned for reviewing a single vocabulary item outside a study session.</summary>
    public const int XpPerStandaloneReview = 2;

    /// <summary>
    /// Returns XP earned for a standalone item review (POST /reviews).
    /// Fixed at 2 XP per review regardless of quality rating.
    /// </summary>
    public static int FromStandaloneReview() => XpPerStandaloneReview;
}
