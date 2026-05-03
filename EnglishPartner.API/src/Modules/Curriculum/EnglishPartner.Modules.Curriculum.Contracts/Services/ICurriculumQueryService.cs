namespace EnglishPartner.Modules.Curriculum.Contracts.Services;

/// <summary>
/// Synchronous Module Interface — allows other modules (e.g. AI) to query Curriculum content context
/// without coupling to Curriculum's Domain or Application layers.
/// Per rules.md §8: cross-module sync calls use Module Interfaces defined in Contracts.
/// </summary>
public interface ICurriculumQueryService
{
    /// <summary>
    /// Fetches the AI evaluation context for a curriculum item (vocabulary, grammar, exercise, etc.).
    /// Returns null if item not found.
    /// </summary>
    Task<CurriculumItemContextDto?> GetItemContextAsync(
        Guid itemId,
        CurriculumItemType itemType,
        CancellationToken cancellationToken = default);
}

public sealed record CurriculumItemContextDto(
    CurriculumItemType ItemType,
    string Question,       // The prompt or item presented to the student
    string CorrectAnswer,  // The expected correct answer
    string? Hint,          // Optional hint (e.g. example sentence)
    string LanguageCode);  // "en"

public enum CurriculumItemType
{
    Vocabulary = 1,
    Grammar = 2,
    Exercise = 3,
    ExampleSentence = 4,
    Conversation = 5,
    Topic = 6,
    ConversationLine = 7
}
