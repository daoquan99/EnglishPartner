using EnglishLiveTutor.Application.Features.Prompts.Contracts;
using EnglishLiveTutor.Application.Modules;
using EnglishLiveTutor.Application.Modules.Catalog;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Prompts.Services;

public sealed class PromptCatalogReader(
    IModuleDbContextFactory<ICatalogDbContext> dbFactory)
    : IPromptCatalogReader
{
    public async Task<PromptCatalogSnapshot> GetActiveAsync(
        string cefrLevel,
        string conversationMode,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var cefrEntity = await db.CefrLevelPrompts
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.CefrLevel == cefrLevel && p.IsActive, ct);

        var modeEntity = await db.ConversationModePrompts
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.ModeCode == conversationMode && m.IsActive, ct);

        return new PromptCatalogSnapshot(
            LevelGuidance: cefrEntity?.LevelGuidance,
            ModeDirective: modeEntity?.RenderDirective(cefrLevel));
    }

    public PromptCatalogFallback GetFallback(
        string cefrLevel,
        string conversationMode) =>
        PromptCatalogDefaults.GetFallback(cefrLevel, conversationMode);
}
