using System.Text.Json;

namespace EnglishLiveTutor.Application.Features.Grammar.Models;

public sealed record GrammarStepDetailDto(
    string Code,
    string CefrLevel,
    int Order,
    string TitleVi,
    string TitleEn,
    string SummaryVi,
    bool IsLevelReview,
    JsonElement Theory,
    string Status,
    decimal? BestScore100,
    int CoreExerciseCount,
    int CommunityExerciseCount,
    int MyExerciseCount,
    string? PrevCode = null,
    string? NextCode = null
);
