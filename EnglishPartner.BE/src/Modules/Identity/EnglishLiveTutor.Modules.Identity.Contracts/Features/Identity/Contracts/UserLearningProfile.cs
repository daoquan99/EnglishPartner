namespace EnglishLiveTutor.Application.Features.Identity.Contracts;

public sealed record UserLearningProfile(
    string? TimeZoneId,
    string? PreferredCefrLevel);
