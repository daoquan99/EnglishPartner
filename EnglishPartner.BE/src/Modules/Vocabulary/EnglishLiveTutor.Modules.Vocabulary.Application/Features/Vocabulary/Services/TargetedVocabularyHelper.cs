using System.Text.RegularExpressions;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Services;

public static class TargetedVocabularyHelper
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    public static string NormalizeTerm(string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return string.Empty;

        ReadOnlySpan<char> span = term.AsSpan().Trim();
        int start = 0;
        while (start < span.Length && IsLeadingTrailingPunctuationOrQuote(span[start]))
        {
            start++;
        }

        int end = span.Length - 1;
        while (end >= start && IsLeadingTrailingPunctuationOrQuote(span[end]))
        {
            end--;
        }

        if (start > end)
            return string.Empty;

        string trimmed = span.Slice(start, end - start + 1).ToString();
        return WhitespaceRegex.Replace(trimmed, " ");
    }

    public static bool IsLeadingTrailingPunctuationOrQuote(char c)
    {
        if (char.IsWhiteSpace(c)) return true;
        if (char.IsPunctuation(c)) return true;
        return c is '“' or '”' or '‘' or '’' or '«' or '»' or '"' or '\'' or '„' or '‚' or '‹' or '›' or '`' or '~' or '^';
    }

    public static int CountWords(string normalizedTerm)
    {
        if (string.IsNullOrWhiteSpace(normalizedTerm))
            return 0;

        return normalizedTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    public static List<string> CleanTargetTerms(IEnumerable<string>? terms)
    {
        if (terms is null)
            return [];

        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var term in terms)
        {
            var normalized = NormalizeTerm(term);
            if (!string.IsNullOrEmpty(normalized) && seen.Add(normalized))
            {
                result.Add(normalized);
            }
        }

        return result;
    }
}
