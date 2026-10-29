namespace EnglishLiveTutor.Application.Features.Grammar.Models;

public sealed record GrammarRoadmapDto(
    IReadOnlyList<GrammarLevelDto> Levels
);

public sealed record GrammarLevelDto(
    string Level,
    bool IsUnlocked,
    bool IsPassed,
    bool CanTakeLevelTest,
    IReadOnlyList<GrammarStepRoadmapItemDto> Steps
);

public sealed record GrammarStepRoadmapItemDto(
    string Code,
    int Order,
    string TitleVi,
    string TitleEn,
    string SummaryVi,
    bool IsLevelReview,
    string Status,
    decimal? BestScore100 = null,
    string? PassedBy = null
);
