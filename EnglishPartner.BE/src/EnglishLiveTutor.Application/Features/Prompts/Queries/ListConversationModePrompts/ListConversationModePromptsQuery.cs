using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Prompts;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Prompts.Queries.ListConversationModePrompts;

public sealed record ListConversationModePromptsQuery(
    string? Category = null,
    string? Search = null,
    int? Page = null,
    int? PageSize = null
) : IQuery<PagedResultDto<ConversationModePromptEntity>>;
