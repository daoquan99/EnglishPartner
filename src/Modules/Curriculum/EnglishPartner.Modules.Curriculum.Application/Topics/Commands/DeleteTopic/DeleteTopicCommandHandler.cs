using EnglishPartner.Modules.Curriculum.Domain;
using EnglishPartner.Modules.Curriculum.Domain.Topics.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Topics.Commands.DeleteTopic;

internal sealed class DeleteTopicCommandHandler(
    ITopicRepository topicRepository) : ICommandHandler<DeleteTopicCommand>
{
    public async Task<Result> Handle(DeleteTopicCommand request, CancellationToken cancellationToken)
    {
        var topic = await topicRepository.GetByIdAsync(request.Id, cancellationToken);
        if (topic is null) return Result.Failure(CurriculumErrors.Topic.NotFound(request.Id));

        topicRepository.Remove(topic);
        return Result.Success();
    }
}
