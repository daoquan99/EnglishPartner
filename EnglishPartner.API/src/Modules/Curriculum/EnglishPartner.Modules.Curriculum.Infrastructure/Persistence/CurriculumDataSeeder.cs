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

        modelBuilder.Entity<EnglishPartner.Modules.Curriculum.Domain.Topics.Topic>().HasData(
            CreateTopic("00000000-0000-0000-0002-000000000001", "Greetings", "en", "Beginner", "Basic greetings and introductions.", 1),
            CreateTopic("00000000-0000-0000-0002-000000000002", "Food & Drink", "en", "Beginner", "Ordering food, drinks, and describing meals.", 2),
            CreateTopic("00000000-0000-0000-0002-000000000003", "Travel", "en", "Intermediate", "Navigating airports, hotels, and transportation.", 3),
            CreateTopic("00000000-0000-0000-0002-000000000004", "Business English", "en", "Advanced", "Professional vocabulary for meetings and emails.", 4),
            CreateTopic("00000000-0000-0000-0002-000000000005", "Everyday Activities", "en", "Elementary", "Talking about daily routines and hobbies.", 5),
            CreateTopic("00000000-0000-0000-0002-000000000006", "Information Technology", "en", "Intermediate", "Vocabulary for IT, hardware, and tech support.", 6),
            CreateTopic("00000000-0000-0000-0002-000000000007", "Software Development", "en", "Advanced", "Coding, software architecture, and agile methodologies.", 7),
            CreateTopic("00000000-0000-0000-0002-000000000008", "Daily Communication", "en", "Elementary", "Small talk, expressing opinions, and casual chats.", 8),
            CreateTopic("00000000-0000-0000-0002-000000000009", "Professional Networking", "en", "Intermediate", "Making connections, presenting ideas, and interviews.", 9));
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

    private static object CreateTopic(string id, string title, string languageCode, string level, string description, int sortOrder) => new
    {
        Id = Guid.Parse(id),
        Title = title,
        Description = description,
        LanguageCode = languageCode,
        Level = level,
        SortOrder = sortOrder,
        ImageUrl = (string?)null,
        IsActive = true,
        IsDeleted = false,
        CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };
}
