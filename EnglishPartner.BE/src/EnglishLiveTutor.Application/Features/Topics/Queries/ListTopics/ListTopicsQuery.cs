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
    string? Category = null
) : IQuery<List<TopicDto>>;

public sealed class ListTopicsQueryHandler : IQueryHandler<ListTopicsQuery, List<TopicDto>>
{
    private readonly ITopicRepository _topicRepo;

    public ListTopicsQueryHandler(ITopicRepository topicRepo)
    {
        _topicRepo = topicRepo;
    }

    public async Task<List<TopicDto>> HandleAsync(ListTopicsQuery query, CancellationToken ct = default)
    {
        var items = await _topicRepo.ListAsync(
            query.OnlyActive,
            query.SupportsConversation,
            query.SupportsVocabulary,
            query.ModeCode,
            query.Category,
            ct);

        return items.Select(MapToDto).ToList();
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
