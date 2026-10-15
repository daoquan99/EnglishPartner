using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Topics;
using EnglishLiveTutor.Domain.Topics.Repositories;

namespace EnglishLiveTutor.Application.Features.Topics.Queries.ListTopics;

public sealed record ListTopicsQuery(
    bool OnlyActive = true,
    bool? SupportsConversation = null,
    bool? SupportsVocabulary = null,
    string? ModeCode = null,
    string? Category = null,
    string? Search = null,
    int? Page = null,
    int? PageSize = null
) : IQuery<PagedResultDto<TopicDto>>;
