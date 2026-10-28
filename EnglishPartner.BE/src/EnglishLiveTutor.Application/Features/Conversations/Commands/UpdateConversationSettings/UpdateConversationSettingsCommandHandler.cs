using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Conversations;
using EnglishLiveTutor.Domain.Conversations.Repositories;

namespace EnglishLiveTutor.Application.Features.Conversations.Commands.UpdateConversationSettings;

public sealed class UpdateConversationSettingsCommandHandler : ICommandHandler<UpdateConversationSettingsCommand, ConversationEntity?>
{
    private readonly IConversationRepository _repo;
    private readonly ICurrentUser _currentUser;

    public UpdateConversationSettingsCommandHandler(IConversationRepository repo, ICurrentUser currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<ConversationEntity?> HandleAsync(UpdateConversationSettingsCommand command, CancellationToken ct = default)
    {
        string userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("Vui lòng đăng nhập.");
        var req = command.Request;
        return await _repo.UpdateConversationSettingsAsync(
            userId: userId,
            id: command.Id,
            title: req.Title,
            cefrLevel: req.CefrLevel,
            extendedThinking: req.ExtendedThinking,
            voiceName: req.VoiceName,
            speechSpeed: req.SpeechSpeed,
            conversationMode: req.ConversationMode,
            topic: req.Topic,
            customSystemPrompt: req.CustomSystemPrompt,
            ct: ct);
    }
}
