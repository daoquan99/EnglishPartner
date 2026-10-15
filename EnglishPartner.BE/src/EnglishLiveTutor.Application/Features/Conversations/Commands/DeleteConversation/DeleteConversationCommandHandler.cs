using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Conversations.Repositories;

namespace EnglishLiveTutor.Application.Features.Conversations.Commands.DeleteConversation;

public sealed class DeleteConversationCommandHandler : ICommandHandler<DeleteConversationCommand, bool>
{
    private readonly IConversationRepository _repo;

    public DeleteConversationCommandHandler(IConversationRepository repo)
    {
        _repo = repo;
    }

    public async Task<bool> HandleAsync(DeleteConversationCommand command, CancellationToken ct = default)
    {
        return await _repo.DeleteConversationAsync(command.Id, ct);
    }
}
