using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.AI.Application.Chat.Commands.UpdateMessageTranslation;

public sealed record UpdateMessageTranslationCommand(
    Guid SessionId,
    Guid MessageId,
    string Translation) : ICommand;
