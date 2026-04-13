using EnglishPartner.Modules.Curriculum.Domain.Languages;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Curriculum.Infrastructure.Persistence;

internal static class CurriculumDataSeeder
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Language>().HasData(
            CreateLanguage("00000000-0000-0000-0001-000000000001", "en", "English", "English", 1),
            CreateLanguage("00000000-0000-0000-0001-000000000002", "vi", "Vietnamese", "Tiếng Việt", 2),
            CreateLanguage("00000000-0000-0000-0001-000000000003", "zh", "Chinese", "中文", 3),
            CreateLanguage("00000000-0000-0000-0001-000000000004", "ja", "Japanese", "日本語", 4),
            CreateLanguage("00000000-0000-0000-0001-000000000005", "ko", "Korean", "한국어", 5));
    }

    private static object CreateLanguage(string id, string code, string name, string nativeName, int sortOrder) => new
    {
        Id = Guid.Parse(id),
        Code = code,
        Name = name,
        NativeName = nativeName,
        SortOrder = sortOrder,
        IsActive = true,
        IsDeleted = false,
        CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };
}
