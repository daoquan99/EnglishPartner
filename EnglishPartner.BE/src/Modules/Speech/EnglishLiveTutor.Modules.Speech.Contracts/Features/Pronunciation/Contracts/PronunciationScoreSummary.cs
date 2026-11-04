namespace EnglishLiveTutor.Application.Features.Pronunciation.Contracts;

public sealed record PronunciationScoreSummary(
    int TotalCount,
    decimal? AverageScore);
