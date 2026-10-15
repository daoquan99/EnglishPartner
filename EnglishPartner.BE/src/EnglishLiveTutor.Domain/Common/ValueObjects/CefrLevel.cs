namespace EnglishLiveTutor.Domain.Common.ValueObjects;

/// <summary>
/// Value object representing a canonical CEFR level (A1, A2, B1, B2, C1, C2).
/// Preserves string wire/storage representation through implicit conversion and Normalize helpers.
/// </summary>
public readonly record struct CefrLevel
{
    private static readonly HashSet<string> ValidCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "A1", "A2", "B1", "B2", "C1", "C2"
    };

    public string Code { get; }

    public CefrLevel(string? raw, bool allowCustomFallback = true)
    {
        string normalized = string.IsNullOrWhiteSpace(raw) ? "B1" : raw.Trim().ToUpperInvariant();
        if (!allowCustomFallback && !ValidCodes.Contains(normalized))
        {
            throw new InvalidOperationException("CEFR phải là A1, A2, B1, B2, C1 hoặc C2.");
        }

        Code = normalized;
    }

    public static string Normalize(string? raw, string defaultLevel = "B1")
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return defaultLevel;
        }

        return raw.Trim().ToUpperInvariant();
    }

    public static string NormalizeStrict(string? raw)
    {
        string normalized = string.IsNullOrWhiteSpace(raw) ? "B1" : raw.Trim().ToUpperInvariant();
        if (!ValidCodes.Contains(normalized))
        {
            throw new InvalidOperationException("CEFR phải là A1, A2, B1, B2, C1 hoặc C2.");
        }

        return normalized;
    }

    public override string ToString() => Code;

    public static implicit operator string(CefrLevel level) => level.Code;
}
