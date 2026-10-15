using System.Text.RegularExpressions;

namespace EnglishLiveTutor.Infrastructure.Realtime;

/// <summary>
/// Deduplicates and merges streaming ASR chunks from Gemini Live.
/// Handles both incremental token chunks (" Home", " Alone") and cumulative
/// revised hypotheses ("Yes, one mile" -> "Yes, one my favorites" -> "Yes, one my favourite comedy movie").
/// </summary>
public static class SmartTextDeduplicator
{
    public static string MergeChunk(string currentAccumulated, string incomingChunk)
    {
        if (string.IsNullOrWhiteSpace(incomingChunk))
            return currentAccumulated;

        string incClean = CollapseNearDuplicateSentences(incomingChunk.Trim());
        if (string.IsNullOrWhiteSpace(currentAccumulated))
            return incClean;

        string curTrim = CollapseNearDuplicateSentences(currentAccumulated.Trim());
        if (string.IsNullOrWhiteSpace(curTrim))
            return incClean;

        // 1. Exact or suffix match -> ignore incoming
        if (curTrim.Equals(incClean, StringComparison.OrdinalIgnoreCase) ||
            curTrim.EndsWith(incClean, StringComparison.OrdinalIgnoreCase))
        {
            return curTrim;
        }

        // 2. Pure prefix extension -> incomingChunk extends curTrim
        if (incClean.StartsWith(curTrim, StringComparison.OrdinalIgnoreCase))
        {
            return incClean;
        }

        // 3. If curTrim starts with incClean (incoming is a shorter earlier prefix) -> keep curTrim
        if (curTrim.StartsWith(incClean, StringComparison.OrdinalIgnoreCase))
        {
            return curTrim;
        }

        var wordsCur = TokenizeWordsWithPositions(curTrim);
        var wordsInc = TokenizeWordsWithPositions(incClean);

        if (wordsCur.Count > 0 && wordsInc.Count > 0)
        {
            // 4. Same-anchor progressive ASR revision of the whole utterance:
            //    e.g. cur="Yes, one mile", inc="Yes, one my favorites"
            //    or cur="Yes, one my favourite comedy movie is Home Alone. I watch",
            //       inc="Yes, one my favorite comedy movie is Home Alone. I uh I have watched"
            if (IsProgressiveRevisionOfWhole(wordsCur, wordsInc))
            {
                return wordsInc.Count >= wordsCur.Count - 1 ? incClean : curTrim;
            }

            // 5. Progressive revision of a tail segment of curTrim:
            //    Find if incClean starts with the same 2+ anchor words as a suffix of curTrim
            //    e.g. cur="... Home Alone. Silhouettes and the Clever"
            //         inc="Silhouettes and the Clever Trick in the movie"
            int tailReplaceCharIdx = FindTailProgressiveAnchorCharIndex(curTrim, wordsCur, wordsInc);
            if (tailReplaceCharIdx >= 0)
            {
                string prefixBeforeTail = curTrim[..tailReplaceCharIdx].TrimEnd();
                string combined = string.IsNullOrWhiteSpace(prefixBeforeTail)
                    ? incClean
                    : $"{prefixBeforeTail} {incClean}";
                return CollapseNearDuplicateSentences(combined);
            }

            // 6. Exact/fuzzy suffix-prefix word overlap:
            //    e.g. cur="I have watched", inc="watched it several times"
            int overlapWords = FindSuffixPrefixWordOverlap(wordsCur, wordsInc);
            if (overlapWords > 0)
            {
                if (overlapWords >= wordsInc.Count)
                    return curTrim;

                int cutCharInInc = wordsInc[overlapWords].StartIndex;
                string remainderInc = incClean[cutCharInInc..].TrimStart();
                string mergedOverlap = string.IsNullOrWhiteSpace(remainderInc)
                    ? curTrim
                    : $"{curTrim} {remainderInc}";
                return CollapseNearDuplicateSentences(mergedOverlap);
            }
        }

        // 7. Full-utterance near-duplicate check (e.g. British vs American spelling re-transcription)
        if (AreNearDuplicateUtterances(curTrim, incClean))
        {
            return incClean.Length >= curTrim.Length ? incClean : curTrim;
        }

        // 8. Normal incremental token append (preserve exact spacing)
        bool needsSpace = !currentAccumulated.EndsWith(' ') &&
                          !incomingChunk.StartsWith(' ') &&
                          !incomingChunk.StartsWith('.') &&
                          !incomingChunk.StartsWith(',') &&
                          !incomingChunk.StartsWith('!') &&
                          !incomingChunk.StartsWith('?') &&
                          !incomingChunk.StartsWith('\'');

        string merged = needsSpace
            ? $"{curTrim} {incClean}"
            : $"{curTrim}{incClean}";

        return CollapseNearDuplicateSentences(merged.Trim());
    }

    private static bool IsProgressiveRevisionOfWhole(
        List<WordSpan> wordsCur,
        List<WordSpan> wordsInc)
    {
        int minCount = Math.Min(wordsCur.Count, wordsInc.Count);
        if (minCount == 0) return false;

        // First word must match (or be near-identical)
        if (!WordsMatch(wordsCur[0].Word, wordsInc[0].Word))
            return false;

        if (minCount == 1)
            return false;

        // If first 2 words match (e.g. "Yes, one mile" vs "Yes, one my favorites"),
        // it is definitely a cumulative ASR revision from the start of the utterance.
        if (WordsMatch(wordsCur[1].Word, wordsInc[1].Word))
            return true;

        // Or if >= 55% of the prefix words match in similar positions
        int matches = 0;
        for (int i = 0; i < minCount; i++)
        {
            int jStart = Math.Max(0, i - 2);
            int jEnd = Math.Min(wordsInc.Count - 1, i + 2);
            for (int j = jStart; j <= jEnd; j++)
            {
                if (WordsMatch(wordsCur[i].Word, wordsInc[j].Word))
                {
                    matches++;
                    break;
                }
            }
        }

        return (double)matches / minCount >= 0.55;
    }

    private static int FindTailProgressiveAnchorCharIndex(
        string curTrim,
        List<WordSpan> wordsCur,
        List<WordSpan> wordsInc)
    {
        if (wordsInc.Count < 2 || wordsCur.Count < 2)
            return -1;

        // Search backwards in wordsCur for a position where wordsCur[pos..pos+1] matches wordsInc[0..1]
        int searchLimit = Math.Max(0, wordsCur.Count - Math.Min(wordsCur.Count, wordsInc.Count + 6));
        for (int pos = searchLimit; pos <= wordsCur.Count - 2; pos++)
        {
            if (WordsMatch(wordsCur[pos].Word, wordsInc[0].Word) &&
                WordsMatch(wordsCur[pos + 1].Word, wordsInc[1].Word))
            {
                int tailLen = wordsCur.Count - pos;
                // Confirm that incoming is revising/extending this tail
                if (wordsInc.Count >= tailLen - 1)
                {
                    return wordsCur[pos].StartIndex;
                }
            }
        }

        return -1;
    }

    private static int FindSuffixPrefixWordOverlap(
        List<WordSpan> wordsCur,
        List<WordSpan> wordsInc)
    {
        int maxPossible = Math.Min(wordsCur.Count, wordsInc.Count);
        for (int k = maxPossible; k >= 1; k--)
        {
            bool allMatch = true;
            for (int i = 0; i < k; i++)
            {
                string wCur = wordsCur[wordsCur.Count - k + i].Word;
                string wInc = wordsInc[i].Word;
                if (!WordsMatch(wCur, wInc))
                {
                    allMatch = false;
                    break;
                }
            }
            if (allMatch)
            {
                // Single-word overlap only if the word is substantive (>= 4 chars) or exact match
                if (k == 1 && wordsInc[0].Word.Length < 4 && !wordsCur[^1].Word.Equals(wordsInc[0].Word, StringComparison.OrdinalIgnoreCase))
                    continue;
                return k;
            }
        }
        return 0;
    }

    public static bool AreNearDuplicateUtterances(string a, string b)
    {
        var wordsA = TokenizeWords(a);
        var wordsB = TokenizeWords(b);

        if (wordsA.Count < 3 || wordsB.Count < 3)
            return false;

        int minCount = Math.Min(wordsA.Count, wordsB.Count);
        int maxCount = Math.Max(wordsA.Count, wordsB.Count);
        if ((double)minCount / maxCount < 0.60)
            return false;

        int matches = 0;
        for (int i = 0; i < minCount; i++)
        {
            int jStart = Math.Max(0, i - 2);
            int jEnd = Math.Min(wordsB.Count - 1, i + 2);
            for (int j = jStart; j <= jEnd; j++)
            {
                if (WordsMatch(wordsA[i], wordsB[j]))
                {
                    matches++;
                    break;
                }
            }
        }

        double similarity = (double)matches / minCount;
        return similarity >= 0.65;
    }

    /// <summary>
    /// Collapses progressive ASR echoes (e.g. "Yes, one mile Yes, one my favorites Yes, one my favourite comedy...")
    /// and repeated sentences inside a single string.
    /// </summary>
    public static string CollapseNearDuplicateSentences(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        string normalized = string.Join(' ', text.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Length < 12)
            return normalized;

        // 1. Collapse progressive echoes where a 2-word anchor repeats and the following slice overlaps
        normalized = CollapseProgressiveAnchorRepeats(normalized);

        // 2. Check if the string is two identical or near-identical halves
        int len = normalized.Length;
        for (int split = len / 2 - 6; split <= len / 2 + 6; split++)
        {
            if (split <= 8 || split >= len - 8) continue;
            if (normalized[split] != ' ') continue;

            string firstHalf = normalized[..split].Trim();
            string secondHalf = normalized[(split + 1)..].Trim();

            if (firstHalf.Equals(secondHalf, StringComparison.OrdinalIgnoreCase) ||
                AreNearDuplicateUtterances(firstHalf, secondHalf))
            {
                normalized = secondHalf.Length >= firstHalf.Length ? secondHalf : firstHalf;
                break;
            }
        }

        // 3. Sentence-by-sentence deduplication on '.', '!', '?'
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
                if (prev.Equals(curr, StringComparison.OrdinalIgnoreCase) ||
                    AreNearDuplicateUtterances(prev, curr))
                {
                    // Keep the fuller/later version
                    if (curr.Length >= prev.Length)
                        deduped[^1] = curr;
                }
                else
                {
                    deduped.Add(curr);
                }
            }
            normalized = string.Join(" ", deduped);
        }

        // 4. Collapse immediate adjacent duplicate words ("watched watched" -> "watched", "I I" -> "I")
        normalized = Regex.Replace(normalized, @"\b(\w+)(\s+\1\b)+", "$1", RegexOptions.IgnoreCase);

        return normalized.Trim();
    }

    private static string CollapseProgressiveAnchorRepeats(string text)
    {
        var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 6)
            return text;

        var cleanTokens = new List<string>();
        var cleanIndices = new List<int>();
        int i = 0;

        while (i < tokens.Length)
        {
            cleanTokens.Add(tokens[i]);
            cleanIndices.Add(i);

            // Check if the last 2 added tokens match a progressive restart within the next 1..40 tokens
            if (cleanTokens.Count >= 2)
            {
                string w1 = StripPunct(cleanTokens[^2]);
                string w2 = StripPunct(cleanTokens[^1]);
                int startIdx = cleanIndices[^2];

                if (!string.IsNullOrEmpty(w1) && !string.IsNullOrEmpty(w2))
                {
                    int restartIdx = -1;
                    int lookaheadLimit = Math.Min(tokens.Length - 1, i + 40);
                    for (int j = i + 1; j < lookaheadLimit; j++)
                    {
                        if (!WordsMatch(w1, StripPunct(tokens[j])) ||
                            !WordsMatch(w2, StripPunct(tokens[j + 1])))
                        {
                            continue;
                        }

                        int spanLen = j - startIdx;
                        int nextSliceEnd = Math.Min(tokens.Length, j + spanLen + 8);
                        var nextSliceSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        for (int k = j; k < nextSliceEnd; k++)
                        {
                            string sw = StripPunct(tokens[k]).Replace("our", "or");
                            if (!string.IsNullOrEmpty(sw))
                                nextSliceSet.Add(sw);
                        }

                        int matchCount = 0;
                        int totalDraftWords = 0;
                        for (int k = startIdx; k < j; k++)
                        {
                            string dw = StripPunct(tokens[k]).Replace("our", "or");
                            if (string.IsNullOrEmpty(dw)) continue;
                            totalDraftWords++;
                            if (nextSliceSet.Contains(dw))
                                matchCount++;
                        }

                        double overlapRatio = totalDraftWords > 0
                            ? (double)matchCount / totalDraftWords
                            : 0.0;

                        if (overlapRatio >= 0.55)
                        {
                            restartIdx = j;
                        }
                    }

                    if (restartIdx > i)
                    {
                        cleanTokens.RemoveAt(cleanTokens.Count - 1);
                        cleanTokens.RemoveAt(cleanTokens.Count - 1);
                        cleanIndices.RemoveAt(cleanIndices.Count - 1);
                        cleanIndices.RemoveAt(cleanIndices.Count - 1);
                        i = restartIdx;
                        continue;
                    }
                }
            }
            i++;
        }

        return string.Join(' ', cleanTokens);
    }

    private static string StripPunct(string raw) =>
        Regex.Replace(raw.ToLowerInvariant(), @"[^a-z0-9']", "");

    private static bool WordsMatch(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return false;
        if (a.Equals(b, StringComparison.OrdinalIgnoreCase))
            return true;
        if (a.Replace("our", "or", StringComparison.OrdinalIgnoreCase)
             .Equals(b.Replace("our", "or", StringComparison.OrdinalIgnoreCase), StringComparison.OrdinalIgnoreCase))
            return true;
        // Handle UK/US spelling or minor 1-char ASR tweak on 5+ char words (e.g. favourite vs favorite)
        if (a.Length >= 5 && b.Length >= 5 && Math.Abs(a.Length - b.Length) <= 2)
        {
            return LevenshteinDistance(a.ToLowerInvariant(), b.ToLowerInvariant()) <= 2;
        }
        return false;
    }

    private sealed record WordSpan(string Word, int StartIndex);

    private static List<WordSpan> TokenizeWordsWithPositions(string s)
    {
        var list = new List<WordSpan>();
        foreach (Match m in Regex.Matches(s, @"[A-Za-z0-9']+"))
        {
            list.Add(new WordSpan(m.Value.ToLowerInvariant(), m.Index));
        }
        return list;
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
