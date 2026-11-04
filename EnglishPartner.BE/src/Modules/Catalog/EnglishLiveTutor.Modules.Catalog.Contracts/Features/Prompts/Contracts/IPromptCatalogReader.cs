namespace EnglishLiveTutor.Application.Features.Prompts.Contracts;

public sealed record PromptCatalogSnapshot(
    string? LevelGuidance,
    string? ModeDirective);

public interface IPromptCatalogReader
{
    Task<PromptCatalogSnapshot> GetActiveAsync(
        string cefrLevel,
        string conversationMode,
        CancellationToken ct = default);

    PromptCatalogFallback GetFallback(
        string cefrLevel,
        string conversationMode);
}
