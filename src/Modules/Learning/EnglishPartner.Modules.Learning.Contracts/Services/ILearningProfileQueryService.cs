namespace EnglishPartner.Modules.Learning.Contracts.Services;

/// <summary>
/// Synchronous Module Interface — allows other modules (e.g. AI) to query Learning Profile metadata
/// without coupling to Learning's Domain or Application layers.
/// Per rules.md §8: cross-module sync calls use Module Interfaces defined in Contracts.
/// </summary>
public interface ILearningProfileQueryService
{
    Task<UserLearningLevelDto?> GetUserLevelAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record UserLearningLevelDto(
    string CurrentLevel,
    string TargetLanguage,
    int TotalXp,
    int CurrentStreak);
