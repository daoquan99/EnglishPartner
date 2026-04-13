using EnglishPartner.Modules.Curriculum.Domain.Topics.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Topics.Queries.GetTopics;

internal sealed class GetTopicsQueryHandler(
    ITopicRepository topicRepository) : IQueryHandler<GetTopicsQuery, List<TopicResponse>>
{
    public async Task<Result<List<TopicResponse>>> Handle(GetTopicsQuery request, CancellationToken cancellationToken)
    {
        var topics = await topicRepository.GetByLanguageCodeAsync(request.LanguageCode, request.SearchTerm, cancellationToken);
        
        return topics.Select(t => new TopicResponse(
            t.Id, t.Title, t.Description, t.Level, t.ImageUrl, t.SortOrder)).ToList();
    }
}
