using EnglishLiveTutor.Domain.Prompts;
using EnglishLiveTutor.Domain.Prompts.CefrLevelPrompts;
using EnglishLiveTutor.Domain.Prompts.ConversationModePrompts;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Persistence.Repositories.Prompts;

public sealed class PromptConfigurationRepository : IPromptConfigurationRepository
{
    private readonly AppDbContext _db;

    public PromptConfigurationRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CefrLevelPromptEntity?> GetCefrLevelPromptAsync(string cefrLevel, CancellationToken ct = default)
    {
        string norm = cefrLevel.Trim().ToUpperInvariant();
        return await _db.CefrLevelPrompts
            .FirstOrDefaultAsync(p => p.CefrLevel == norm, ct);
    }

    public async Task<List<CefrLevelPromptEntity>> ListCefrLevelPromptsAsync(CancellationToken ct = default)
    {
        return await _db.CefrLevelPrompts
            .AsNoTracking()
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync(ct);
    }

    public async Task<ConversationModePromptEntity?> GetConversationModePromptAsync(string modeCode, CancellationToken ct = default)
    {
        string norm = modeCode.Trim().ToLowerInvariant();
        return await _db.ConversationModePrompts
            .FirstOrDefaultAsync(m => m.ModeCode == norm, ct);
    }

    public async Task<List<ConversationModePromptEntity>> ListConversationModePromptsAsync(CancellationToken ct = default)
    {
        return await _db.ConversationModePrompts
            .AsNoTracking()
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync(ct);
    }

    public Task AddCefrLevelPromptAsync(CefrLevelPromptEntity entity, CancellationToken ct = default)
    {
        _db.CefrLevelPrompts.Add(entity);
        return Task.CompletedTask;
    }

    public Task AddConversationModePromptAsync(ConversationModePromptEntity entity, CancellationToken ct = default)
    {
        _db.ConversationModePrompts.Add(entity);
        return Task.CompletedTask;
    }
}
