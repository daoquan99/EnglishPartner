using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Topics.Repositories;

namespace EnglishLiveTutor.Application.Features.Topics.Commands.DeleteTopic;

public sealed record DeleteTopicCommand(Guid Id) : ICommand<bool>;

public sealed class DeleteTopicCommandHandler : ICommandHandler<DeleteTopicCommand, bool>
{
    private readonly ITopicRepository _topicRepo;

    public DeleteTopicCommandHandler(ITopicRepository topicRepo)
    {
        _topicRepo = topicRepo;
    }

    public Task<bool> HandleAsync(DeleteTopicCommand command, CancellationToken ct = default) =>
        _topicRepo.DeleteAsync(command.Id, ct);
}
