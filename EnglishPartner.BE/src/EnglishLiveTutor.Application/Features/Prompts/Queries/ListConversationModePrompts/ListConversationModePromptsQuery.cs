using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Prompts;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Prompts.Queries.ListConversationModePrompts;

public sealed record ListConversationModePromptsQuery(string? Category = null) : IQuery<List<ConversationModePromptEntity>>;

public sealed class ListConversationModePromptsQueryHandler : IQueryHandler<ListConversationModePromptsQuery, List<ConversationModePromptEntity>>
{
    private readonly IAppDbContextFactory _dbFactory;

    public ListConversationModePromptsQueryHandler(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<ConversationModePromptEntity>> HandleAsync(ListConversationModePromptsQuery query, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var q = db.ConversationModePrompts.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            string cat = query.Category.Trim();
            q = q.Where(m => m.Category == cat);
        }

        return await q.OrderBy(m => m.DisplayOrder).ToListAsync(ct);
    }
}
