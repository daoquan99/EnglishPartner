using EnglishPartner.Modules.Curriculum.Application.Topics.Queries.GetTopics;
using EnglishPartner.Modules.Curriculum.Domain;
using EnglishPartner.Modules.Curriculum.Domain.Topics.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Topics.Queries.GetTopicById;

internal sealed class GetTopicByIdQueryHandler(
    ITopicRepository topicRepository) : IQueryHandler<GetTopicByIdQuery, TopicResponse>
{
    public async Task<Result<TopicResponse>> Handle(GetTopicByIdQuery request, CancellationToken cancellationToken)
    {
        var topic = await topicRepository.GetByIdAsync(request.Id, cancellationToken);
        if (topic is null) return Result.Failure<TopicResponse>(CurriculumErrors.Topic.NotFound(request.Id));

        return new TopicResponse(
            topic.Id, topic.Title, topic.Description, topic.Level, topic.ImageUrl, topic.SortOrder);
    }
}
