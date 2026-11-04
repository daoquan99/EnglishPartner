namespace EnglishLiveTutor.Infrastructure.Storage;

public sealed class LessonMediaStorageOptions
{
    public const string SectionName = "Storage:LessonMedia";

    public string RootPath { get; set; } = string.Empty;
    public long MaxFileSizeBytes { get; set; } = 25 * 1024 * 1024; // 25 MB
}
