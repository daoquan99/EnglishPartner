using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Topics.Queries.ListTopics;
using EnglishLiveTutor.Domain.Topics.Repositories;

namespace EnglishLiveTutor.Application.Features.Topics.Queries.GetTopicById;

public sealed record GetTopicByIdQuery(Guid Id) : IQuery<TopicDto?>;

public sealed class GetTopicByIdQueryHandler : IQueryHandler<GetTopicByIdQuery, TopicDto?>
{
    private readonly ITopicRepository _topicRepo;

    public GetTopicByIdQueryHandler(ITopicRepository topicRepo)
    {
        _topicRepo = topicRepo;
    }

    public async Task<TopicDto?> HandleAsync(GetTopicByIdQuery query, CancellationToken ct = default)
    {
        var entity = await _topicRepo.GetByIdAsync(query.Id, ct);
        return entity is null ? null : ListTopicsQueryHandler.MapToDto(entity);
    }
}
