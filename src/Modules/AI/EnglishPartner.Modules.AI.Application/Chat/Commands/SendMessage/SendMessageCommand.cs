using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.AI.Application.Chat.Commands.SendMessage;

public sealed record SendMessageCommand(
    Guid SessionId,
    Guid UserId,
    string Text) : ICommand<SendMessageResponse>;

public sealed record SendMessageResponse(
    Guid MessageId,
    string ModelReply);
