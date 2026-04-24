using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.AI.Application.Chat.Commands.StartChat;

public sealed record StartChatCommand(
    string Title,
    string? SystemPrompt = null) : ICommand<Guid>;
