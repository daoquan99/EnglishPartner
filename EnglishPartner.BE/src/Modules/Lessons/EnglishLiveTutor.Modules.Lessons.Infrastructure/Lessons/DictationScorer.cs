using System.Text;
using System.Text.RegularExpressions;
using EnglishLiveTutor.Application.Common.Interfaces;

namespace EnglishLiveTutor.Infrastructure.Lessons;

/// <summary>
/// Implements the canonical word-level Levenshtein Distance scoring algorithm for Dictation exercises.
/// Normalization Policy:
/// 1. Trim & Unicode NFC
/// 2. ToLowerInvariant
/// 3. Remove punctuation: . , ! ? ; : ' " ( ) [ ] -
/// 4. Collapse contiguous whitespace to single space
/// 5. Tokenize into words by single space
/// </summary>
public sealed class DictationScorer : IDictationScorer
{
    private static readonly Regex PunctuationRegex = new(
        @"[\.,!\?;:'""\(\)\[\]\-]",
        RegexOptions.Compiled);

    private static readonly Regex WhitespaceRegex = new(
        @"\s+",
        RegexOptions.Compiled);

    public DictationScoreResult Score(string referenceTranscript, string learnerTranscript, decimal maxPoints)
    {
        var refTokens = TokenizeWords(referenceTranscript);
        var learnerTokens = TokenizeWords(learnerTranscript);

        int n = refTokens.Length;
        int m = learnerTokens.Length;

        int distance = ComputeWordLevenshtein(refTokens, learnerTokens);

        decimal wordAccuracy;
        if (n == 0)
        {
            wordAccuracy = m == 0 ? 1.0m : 0.0m;
        }
        else
        {
            wordAccuracy = Math.Max(0.0m, (decimal)(n - distance) / n);
        }

        decimal score100 = Math.Round(wordAccuracy * 100.00m, 2, MidpointRounding.AwayFromZero);
        decimal pointsEarned = Math.Round((score100 / 100.00m) * maxPoints, 2, MidpointRounding.AwayFromZero);

        return new DictationScoreResult(
            ReferenceWordCount: n,
            LearnerWordCount: m,
            WordLevenshteinDistance: distance,
            WordAccuracy: wordAccuracy,
            Score100: score100,
            PointsEarned: pointsEarned);
    }

    public static string[] TokenizeWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();

        // 1. Unicode NFC & Trim
        string normalized = text.Trim().Normalize(NormalizationForm.FormC);

        // 2. ToLowerInvariant
        normalized = normalized.ToLowerInvariant();

        // 3. Remove punctuation
        normalized = PunctuationRegex.Replace(normalized, " ");

        // 4. Collapse whitespace
        normalized = WhitespaceRegex.Replace(normalized, " ").Trim();

        if (string.IsNullOrEmpty(normalized))
            return Array.Empty<string>();

        return normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    private static int ComputeWordLevenshtein(string[] r, string[] l)
    {
        int n = r.Length;
        int m = l.Length;

        if (n == 0) return m;
        if (m == 0) return n;

        var dp = new int[n + 1, m + 1];

        for (int i = 0; i <= n; i++)
            dp[i, 0] = i;

        for (int j = 0; j <= m; j++)
            dp[0, j] = j;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = string.Equals(r[i - 1], l[j - 1], StringComparison.Ordinal) ? 0 : 1;

                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost);
            }
        }

        return dp[n, m];
    }
}
