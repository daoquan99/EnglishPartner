using EnglishPartner.Modules.Curriculum.Domain;
using EnglishPartner.Modules.Curriculum.Domain.Topics.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Topics.Commands.DeleteTopic;

internal sealed class DeleteTopicCommandHandler(
    ITopicRepository topicRepository,
    ICurrentUserService currentUser,
    TimeProvider timeProvider) : ICommandHandler<DeleteTopicCommand>
{
    public async Task<Result> Handle(DeleteTopicCommand request, CancellationToken cancellationToken)
    {
        var topic = await topicRepository.GetByIdAsync(request.Id, cancellationToken);
        if (topic is null) return Result.Failure(CurriculumErrors.Topic.NotFound(request.Id));

        topicRepository.Remove(topic, currentUser.UserId, timeProvider.GetUtcNow().UtcDateTime);
        return Result.Success();
    }
}
