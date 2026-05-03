namespace EnglishPartner.SharedKernel.Application.Caching;

/// <summary>
/// Centralized location for all Cache Keys used across the application.
/// </summary>
public static class CacheKeys
{
    public static class Identity
    {
        public static string Session(string sessionId) => $"session:{sessionId}";
        public static string UserSessions(Guid userId) => $"user:{userId}:sessions";
        public static string PasswordReset(Guid userId) => $"password-reset:{userId}";
    }

    public static class Curriculum
    {
        public static string VocabulariesPrefix(string languageCode) => $"curriculum:vocabularies:{languageCode}:";
        public static string VocabulariesQuery(string languageCode, int page, int pageSize, string? level, Guid? topicId) 
            => $"{VocabulariesPrefix(languageCode)}p{page}:s{pageSize}:lvl{level ?? "all"}:topic{topicId?.ToString() ?? "all"}";

        public static string ExampleSentencesPrefix(string languageCode) => $"curriculum:examples:{languageCode}:";
        public static string ExampleSentencesQuery(string languageCode, int page, int pageSize, Guid? vocabId, Guid? grammarId, Guid? topicId) 
            => $"{ExampleSentencesPrefix(languageCode)}p{page}:s{pageSize}:v{vocabId?.ToString() ?? "all"}:g{grammarId?.ToString() ?? "all"}:t{topicId?.ToString() ?? "all"}";

        public static string ConversationsPrefix(string languageCode) => $"curriculum:conversations:{languageCode}:";
        public static string ConversationsQuery(string languageCode, int page, int pageSize, Guid? topicId, string? level)
            => $"{ConversationsPrefix(languageCode)}p{page}:s{pageSize}:topic{topicId?.ToString() ?? "all"}:lvl{level ?? "all"}";

        public static string ExercisesPrefix(string languageCode) => $"curriculum:exercises:{languageCode}:";
        public static string ExercisesQuery(string languageCode, int page, int pageSize, Guid? topicId, Guid? vocabularyId, Guid? grammarId, string? level, int? exerciseType)
            => $"{ExercisesPrefix(languageCode)}p{page}:s{pageSize}:t{topicId?.ToString() ?? "all"}:v{vocabularyId?.ToString() ?? "all"}:g{grammarId?.ToString() ?? "all"}:lvl{level ?? "all"}:et{exerciseType?.ToString() ?? "all"}";
    }

    public static class AI
    {
        public static string GeminiTts(string textHash, string voiceId) => $"ai:tts:{textHash}:{voiceId}";
    }

    public static class Admin
    {
        public const string DashboardStats = "admin:dashboard_stats";
    }
}
