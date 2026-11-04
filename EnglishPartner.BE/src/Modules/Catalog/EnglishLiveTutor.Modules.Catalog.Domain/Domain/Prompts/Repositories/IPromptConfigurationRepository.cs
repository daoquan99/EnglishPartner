namespace EnglishLiveTutor.Domain.Prompts.Repositories;

public interface IPromptConfigurationRepository
{
    Task<CefrLevelPromptEntity?> GetCefrLevelPromptAsync(string cefrLevel, CancellationToken ct = default);
    Task<List<CefrLevelPromptEntity>> ListCefrLevelPromptsAsync(CancellationToken ct = default);
    Task<ConversationModePromptEntity?> GetConversationModePromptAsync(string modeCode, CancellationToken ct = default);
    Task<List<ConversationModePromptEntity>> ListConversationModePromptsAsync(CancellationToken ct = default);
    Task AddCefrLevelPromptAsync(CefrLevelPromptEntity entity, CancellationToken ct = default);
    Task AddConversationModePromptAsync(ConversationModePromptEntity entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
