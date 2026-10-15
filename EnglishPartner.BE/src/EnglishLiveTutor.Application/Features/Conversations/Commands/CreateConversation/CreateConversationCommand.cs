using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Conversations;
using EnglishLiveTutor.Domain.Conversations.Repositories;

namespace EnglishLiveTutor.Application.Features.Conversations.Commands.CreateConversation;

public sealed record CreateConversationCommand(CreateConversationRequest Request) : ICommand<ConversationEntity>;

public sealed class CreateConversationCommandHandler : ICommandHandler<CreateConversationCommand, ConversationEntity>
{
    private readonly IConversationRepository _repo;

    public CreateConversationCommandHandler(IConversationRepository repo)
    {
        _repo = repo;
    }

    public async Task<ConversationEntity> HandleAsync(CreateConversationCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        return await _repo.CreateOrUpdateConversationAsync(
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
