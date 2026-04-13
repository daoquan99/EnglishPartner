using EnglishPartner.Modules.Curriculum.Domain.Topics;
using EnglishPartner.Modules.Curriculum.Domain.Topics.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Topics.Commands.CreateTopic;

internal sealed class CreateTopicCommandHandler(
    ITopicRepository topicRepository) : ICommandHandler<CreateTopicCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTopicCommand request, CancellationToken cancellationToken)
    {
        var topic = Topic.Create(
            request.Title,
            request.LanguageCode,
            request.Description,
            request.Level,
            request.ImageUrl,
            request.SortOrder);

        topicRepository.Add(topic);
        return topic.Id;
    }
}
