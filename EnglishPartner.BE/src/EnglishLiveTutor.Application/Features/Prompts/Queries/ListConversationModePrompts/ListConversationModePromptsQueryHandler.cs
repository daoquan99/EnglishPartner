using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Prompts;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Prompts.Queries.ListConversationModePrompts;

public sealed class ListConversationModePromptsQueryHandler : IQueryHandler<ListConversationModePromptsQuery, PagedResultDto<ConversationModePromptEntity>>
{
    private readonly IAppDbContextFactory _dbFactory;

    public ListConversationModePromptsQueryHandler(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<PagedResultDto<ConversationModePromptEntity>> HandleAsync(ListConversationModePromptsQuery query, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var q = db.ConversationModePrompts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            string cat = query.Category.Trim();
            q = q.Where(m => m.Category == cat);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim().ToLowerInvariant();
            q = q.Where(m =>
                m.ModeCode.ToLower().Contains(term) ||
                m.Category.ToLower().Contains(term) ||
                m.DisplayName.ToLower().Contains(term) ||
                m.DirectiveTemplate.ToLower().Contains(term));
        }

        var ordered = q.OrderBy(m => m.DisplayOrder);
        return await PagedResultDto<ConversationModePromptEntity>.CreateAsync(ordered, query.Page, query.PageSize, ct);
    }
}
