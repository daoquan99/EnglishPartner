using System.Text.RegularExpressions;

namespace EnglishLiveTutor.Infrastructure.Realtime;

/// <summary>
/// Prevents duplicate or near-duplicate sentences from being concatenated twice
/// inside a single chat bubble.
/// </summary>
public static class SmartTextDeduplicator
{
    public static string MergeChunk(string currentAccumulated, string incomingChunk)
    {
        if (string.IsNullOrWhiteSpace(incomingChunk))
            return currentAccumulated;

        if (string.IsNullOrWhiteSpace(currentAccumulated))
            return CollapseNearDuplicateSentences(incomingChunk.Trim());

        string curTrim = currentAccumulated.Trim();
        string incTrim = incomingChunk.Trim();

        // 1. Exact or suffix match -> ignore
        if (curTrim.Equals(incTrim, StringComparison.OrdinalIgnoreCase) ||
            curTrim.EndsWith(incTrim, StringComparison.OrdinalIgnoreCase))
        {
            return curTrim;
        }

        // 2. If incomingChunk starts with curTrim -> replace with incomingChunk
        if (incTrim.StartsWith(curTrim, StringComparison.OrdinalIgnoreCase))
        {
            return CollapseNearDuplicateSentences(incTrim);
        }

        // 3. If both strings are full sentences (4+ words) and share >= 68% of the same words,
        //    incomingChunk is a full-utterance re-transcription (e.g. from a second hub), NOT a new clause!
        //    Keep curTrim (the verbatim original transcript) so we never double-append!
        if (AreNearDuplicateUtterances(curTrim, incTrim))
        {
            return curTrim;
        }

        // 4. Normal incremental token append (preserve exact spacing)
        bool needsSpace = !currentAccumulated.EndsWith(' ') &&
                          !incomingChunk.StartsWith(' ') &&
                          !incomingChunk.StartsWith('.') &&
                          !incomingChunk.StartsWith(',') &&
                          !incomingChunk.StartsWith('!') &&
                          !incomingChunk.StartsWith('?') &&
                          !incomingChunk.StartsWith('\'');

        string merged = needsSpace
            ? $"{currentAccumulated} {incomingChunk}"
            : $"{currentAccumulated}{incomingChunk}";

        return CollapseNearDuplicateSentences(merged.Trim());
    }

    public static bool AreNearDuplicateUtterances(string a, string b)
    {
        var wordsA = TokenizeWords(a);
        var wordsB = TokenizeWords(b);

        if (wordsA.Count < 4 || wordsB.Count < 4)
            return false;

        int minCount = Math.Min(wordsA.Count, wordsB.Count);
        int maxCount = Math.Max(wordsA.Count, wordsB.Count);
        if ((double)minCount / maxCount < 0.65)
            return false;

        int matches = 0;
        for (int i = 0; i < minCount; i++)
        {
            if (wordsA[i].Equals(wordsB[i], StringComparison.OrdinalIgnoreCase) ||
                LevenshteinDistance(wordsA[i], wordsB[i]) <= 2)
            {
                matches++;
            }
        }

        double similarity = (double)matches / maxCount;
        return similarity >= 0.68;
    }

    public static string CollapseNearDuplicateSentences(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length < 20)
            return text;

        string normalized = string.Join(' ', text.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        int len = normalized.Length;

        // 1. Check if the string is two identical or near-identical halves
        for (int split = len / 2 - 4; split <= len / 2 + 4; split++)
        {
            if (split <= 8 || split >= len - 8) continue;
            if (normalized[split] != ' ') continue;

            string firstHalf = normalized[..split].Trim();
            string secondHalf = normalized[(split + 1)..].Trim();

            if (firstHalf.Equals(secondHalf, StringComparison.OrdinalIgnoreCase) ||
                AreNearDuplicateUtterances(firstHalf, secondHalf))
            {
                return firstHalf;
            }
        }

        // 2. Check sentence-by-sentence split on '.', '!', '?'
        var sentences = Regex.Split(normalized, @"(?<=[.!?])\s+")
                             .Where(s => !string.IsNullOrWhiteSpace(s))
                             .ToList();

        if (sentences.Count >= 2)
        {
            var deduped = new List<string> { sentences[0] };
            for (int i = 1; i < sentences.Count; i++)
            {
                string prev = deduped[^1];
                string curr = sentences[i];
                if (!prev.Equals(curr, StringComparison.OrdinalIgnoreCase) &&
                    !AreNearDuplicateUtterances(prev, curr))
                {
                    deduped.Add(curr);
                }
            }
            return string.Join(" ", deduped);
        }

        return normalized;
    }

    private static List<string> TokenizeWords(string s) =>
        Regex.Matches(s.ToLowerInvariant(), @"[a-z']+").Select(m => m.Value).ToList();

    private static int LevenshteinDistance(string s, string t)
    {
        int n = s.Length, m = t.Length;
        if (n == 0) return m;
        if (m == 0) return n;
        var d = new int[n + 1, m + 1];
        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }
}
