namespace EnglishLiveTutor.Application.Common.Models;

public sealed record BuildVocabularyGenerationPromptRequest(
    int RequestedCount = 5,
    string CefrLevel = "B1",
    string? Topic = null,
    List<string>? ExcludedWords = null
);

public sealed record VocabularyGenerationPromptDto(
    string Prompt,
    string PromptVersion,
    int RequestedCount,
    string CefrLevel,
    string Topic,
    IReadOnlyList<string> ExcludedWords
);

public sealed record ImportVocabularyJsonRequest(
    string JsonText,
    string ModelCode = "manual-web-ai",
    string CefrLevel = "B1",
    string? Topic = null,
    bool AssignToTodayPlan = true
);
