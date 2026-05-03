using EnglishPartner.Modules.Curriculum.Domain;
using EnglishPartner.Modules.Curriculum.Domain.Topics.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Topics.Commands.UpdateTopic;

internal sealed class UpdateTopicCommandHandler(
    ITopicRepository topicRepository) : ICommandHandler<UpdateTopicCommand>
{
    public async Task<Result> Handle(UpdateTopicCommand request, CancellationToken cancellationToken)
    {
        var topic = await topicRepository.GetByIdAsync(request.Id, cancellationToken);
        if (topic is null) return Result.Failure(CurriculumErrors.Topic.NotFound(request.Id));

        topic.Update(request.Title, request.Description, request.Level, request.ImageUrl, request.SortOrder);
        topicRepository.Update(topic);
        
        return Result.Success();
    }
}
