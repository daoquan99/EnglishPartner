namespace EnglishLiveTutor.Domain.Common.ValueObjects;

/// <summary>
/// Resolves local yyyy-MM-dd learning dates deterministically from an injected UTC timestamp and IANA/Windows TimeZoneId.
/// </summary>
public static class LearningDate
{
    public const string DefaultTimeZoneId = "Asia/Ho_Chi_Minh";

    public static string NormalizeTimeZoneId(string? timeZoneId) =>
        string.IsNullOrWhiteSpace(timeZoneId) ? DefaultTimeZoneId : timeZoneId.Trim();

    public static string ResolveLocalDateString(string? timeZoneId, DateTimeOffset utcNow)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(timeZoneId))
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
                var localNow = TimeZoneInfo.ConvertTime(utcNow, tz);
                return localNow.ToString("yyyy-MM-dd");
            }
        }
        catch
        {
            // Fallback to UTC+7 (Asia/Ho_Chi_Minh) when OS timezone database lacks the requested ID
        }

        return utcNow.ToOffset(TimeSpan.FromHours(7)).ToString("yyyy-MM-dd");
    }
}
