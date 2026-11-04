using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Topics;
using EnglishLiveTutor.Domain.Topics.Repositories;

namespace EnglishLiveTutor.Application.Features.Topics.Queries.ListTopics;

public sealed class ListTopicsQueryHandler : IQueryHandler<ListTopicsQuery, PagedResultDto<TopicDto>>
{
    private readonly ITopicRepository _topicRepo;

    public ListTopicsQueryHandler(ITopicRepository topicRepo)
    {
        _topicRepo = topicRepo;
    }

    public async Task<PagedResultDto<TopicDto>> HandleAsync(ListTopicsQuery query, CancellationToken ct = default)
    {
        var (items, totalCount) = await _topicRepo.ListPagedAsync(
            query.OnlyActive,
            query.SupportsConversation,
            query.SupportsVocabulary,
            query.ModeCode,
            query.Category,
            query.Search,
            query.Page,
            query.PageSize,
            ct);

        var dtos = items.Select(MapToDto).ToList();
        return PagedResultDto<TopicDto>.FromPage(dtos, totalCount, query.Page, query.PageSize);
    }

    internal static TopicDto MapToDto(TopicEntity t) => new(
        t.Id,
        t.Code,
        t.Name,
        t.DisplayNameVi,
        t.Category,
        t.ModeCode,
        t.SuggestedCefrLevel,
        t.SupportsConversation,
        t.SupportsVocabulary,
        t.Description,
        t.IsFeatured,
        t.IsActive,
        t.DisplayOrder
    );
}
