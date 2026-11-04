using System.Text.RegularExpressions;
using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Grammar;

public sealed class GrammarStepEntity : AggregateRoot<Guid>
{
    private static readonly Regex StepCodeRegex = new(@"^(A1|A2|B1|B2|C1|C2)-(\d{2})$", RegexOptions.Compiled);

    public static readonly string[] Levels = ["A1", "A2", "B1", "B2", "C1", "C2"];

    public string Code { get; private set; } = string.Empty;
    public string CefrLevel { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public string TitleVi { get; private set; } = string.Empty;
    public string TitleEn { get; private set; } = string.Empty;
    public string SummaryVi { get; private set; } = string.Empty;
    public string TheoryJson { get; private set; } = "{}";
    public int TheoryVersion { get; private set; } = 1;
    public bool TheoryEditedByAdmin { get; private set; }
    public bool IsLevelReview { get; private set; }
    public bool IsPublished { get; private set; } = true;
    public byte[]? RowVersion { get; private set; }

    private GrammarStepEntity()
    {
    }

    public GrammarStepEntity(
        string code,
        string cefrLevel,
        int order,
        string titleVi,
        string titleEn,
        string summaryVi,
        string theoryJson,
        int theoryVersion = 1,
        bool isLevelReview = false,
        bool isPublished = true,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        ValidateAndSetCodeAndLevel(code, cefrLevel, order);
        ValidateAndSetMetadata(titleVi, titleEn, summaryVi);
        ValidateAndSetTheory(theoryJson, theoryVersion);

        IsLevelReview = isLevelReview;
        IsPublished = isPublished;
        TheoryEditedByAdmin = false;
    }

    public static GrammarStepEntity Create(
        string code,
        string cefrLevel,
        int order,
        string titleVi,
        string titleEn,
        string summaryVi,
        string theoryJson,
        int theoryVersion = 1,
        bool isLevelReview = false,
        bool isPublished = true,
        Guid? id = null) =>
        new(code, cefrLevel, order, titleVi, titleEn, summaryVi, theoryJson, theoryVersion, isLevelReview, isPublished, id);

    public void UpdateMetadata(string titleVi, string titleEn, string summaryVi, bool isPublished)
    {
        ValidateAndSetMetadata(titleVi, titleEn, summaryVi);
        IsPublished = isPublished;
    }

    public bool ApplyPackTheory(string theoryJson, int theoryVersion)
    {
        if (TheoryEditedByAdmin || theoryVersion <= TheoryVersion)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(theoryJson))
        {
            throw new ArgumentException("TheoryJson cannot be empty.", nameof(theoryJson));
        }

        TheoryJson = theoryJson.Trim();
        TheoryVersion = theoryVersion;
        return true;
    }

    public void EditTheoryByAdmin(string theoryJson)
    {
        if (string.IsNullOrWhiteSpace(theoryJson))
        {
            throw new ArgumentException("TheoryJson cannot be empty.", nameof(theoryJson));
        }

        TheoryJson = theoryJson.Trim();
        TheoryEditedByAdmin = true;
        TheoryVersion++;
    }

    private void ValidateAndSetCodeAndLevel(string code, string cefrLevel, int order)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code is required.", nameof(code));
        }

        var trimmedCode = code.Trim();
        var match = StepCodeRegex.Match(trimmedCode);
        if (!match.Success)
        {
            throw new ArgumentException($"Code '{code}' does not match required pattern '^(A1|A2|B1|B2|C1|C2)-\\d{{2}}$'.", nameof(code));
        }

        var codePrefix = match.Groups[1].Value;
        var codeNumber = int.Parse(match.Groups[2].Value);

        var normalizedCefr = (cefrLevel ?? string.Empty).Trim();
        if (!string.Equals(codePrefix, normalizedCefr, StringComparison.Ordinal))
        {
            throw new ArgumentException($"CefrLevel '{cefrLevel}' does not match code prefix '{codePrefix}'.", nameof(cefrLevel));
        }

        if (order < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(order), "Order must be at least 1.");
        }

        if (codeNumber != order)
        {
            throw new ArgumentException($"Order '{order}' does not match number in code '{codeNumber}'.", nameof(order));
        }

        Code = trimmedCode;
        CefrLevel = normalizedCefr;
        Order = order;
    }

    private void ValidateAndSetMetadata(string titleVi, string titleEn, string summaryVi)
    {
        if (string.IsNullOrWhiteSpace(titleVi))
        {
            throw new ArgumentException("TitleVi is required.", nameof(titleVi));
        }

        if (titleVi.Trim().Length > 200)
        {
            throw new ArgumentException("TitleVi must not exceed 200 characters.", nameof(titleVi));
        }

        if (string.IsNullOrWhiteSpace(titleEn))
        {
            throw new ArgumentException("TitleEn is required.", nameof(titleEn));
        }

        if (titleEn.Trim().Length > 200)
        {
            throw new ArgumentException("TitleEn must not exceed 200 characters.", nameof(titleEn));
        }

        if (string.IsNullOrWhiteSpace(summaryVi))
        {
            throw new ArgumentException("SummaryVi is required.", nameof(summaryVi));
        }

        if (summaryVi.Trim().Length > 500)
        {
            throw new ArgumentException("SummaryVi must not exceed 500 characters.", nameof(summaryVi));
        }

        TitleVi = titleVi.Trim();
        TitleEn = titleEn.Trim();
        SummaryVi = summaryVi.Trim();
    }

    private void ValidateAndSetTheory(string theoryJson, int theoryVersion)
    {
        if (string.IsNullOrWhiteSpace(theoryJson))
        {
            throw new ArgumentException("TheoryJson is required.", nameof(theoryJson));
        }

        if (theoryVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(theoryVersion), "TheoryVersion must be at least 1.");
        }

        TheoryJson = theoryJson.Trim();
        TheoryVersion = theoryVersion;
    }

    public static string? PreviousLevel(string level)
    {
        var norm = (level ?? string.Empty).Trim().ToUpperInvariant();
        var idx = Array.IndexOf(Levels, norm);
        if (idx <= 0)
        {
            return null;
        }

        return Levels[idx - 1];
    }

    public static string? NextLevel(string level)
    {
        var norm = (level ?? string.Empty).Trim().ToUpperInvariant();
        var idx = Array.IndexOf(Levels, norm);
        if (idx < 0 || idx >= Levels.Length - 1)
        {
            return null;
        }

        return Levels[idx + 1];
    }
}
