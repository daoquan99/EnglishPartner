using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Conversations.Repositories;

namespace EnglishLiveTutor.Application.Features.Conversations.Commands.DeleteConversation;

public sealed class DeleteConversationCommandHandler : ICommandHandler<DeleteConversationCommand, bool>
{
    private readonly IConversationRepository _repo;
    private readonly ICurrentUser _currentUser;

    public DeleteConversationCommandHandler(IConversationRepository repo, ICurrentUser currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<bool> HandleAsync(DeleteConversationCommand command, CancellationToken ct = default)
    {
        string userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("Vui lòng đăng nhập.");
        return await _repo.DeleteConversationAsync(userId, command.Id, ct);
    }
}
