using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.AI.Application.Chat.Commands.StartChat;

public sealed record StartChatCommand(
    Guid UserId,
    string Title,
    string? SystemPrompt = null) : ICommand<Guid>;
