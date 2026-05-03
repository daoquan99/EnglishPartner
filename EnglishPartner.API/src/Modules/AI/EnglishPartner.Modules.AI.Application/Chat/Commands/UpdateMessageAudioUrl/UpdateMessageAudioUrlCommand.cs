using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.AI.Application.Chat.Commands.UpdateMessageAudioUrl;

public sealed record UpdateMessageAudioUrlCommand(
    Guid SessionId,
    Guid MessageId,
    string AudioUrl) : ICommand;
