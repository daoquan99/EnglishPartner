using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Conversations;
using EnglishLiveTutor.Domain.Conversations.Repositories;

namespace EnglishLiveTutor.Application.Features.Conversations.Commands.CreateConversation;

public sealed class CreateConversationCommandHandler : ICommandHandler<CreateConversationCommand, ConversationEntity>
{
    private readonly IConversationRepository _repo;
    private readonly ICurrentUser _currentUser;

    public CreateConversationCommandHandler(IConversationRepository repo, ICurrentUser currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<ConversationEntity> HandleAsync(CreateConversationCommand command, CancellationToken ct = default)
    {
        string userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("Vui lòng đăng nhập.");
        var req = command.Request;
        return await _repo.CreateConversationAsync(
            userId: userId,
            id: null,
            req.Title,
            req.CefrLevel,
            req.ExtendedThinking,
            req.VoiceName,
            req.SpeechSpeed,
            req.ConversationMode,
            req.Topic,
            req.CustomSystemPrompt,
            ct);
    }
}
