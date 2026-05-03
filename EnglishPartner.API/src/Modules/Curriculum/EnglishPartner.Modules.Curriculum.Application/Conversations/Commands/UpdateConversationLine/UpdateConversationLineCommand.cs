using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.UpdateConversationLine;

public sealed record UpdateConversationLineCommand(
    Guid ConversationId,
    Guid LineId,
    string Speaker,
    string Text,
    string? AudioUrl,
    Dictionary<string, string>? Translations) : ICommand;
