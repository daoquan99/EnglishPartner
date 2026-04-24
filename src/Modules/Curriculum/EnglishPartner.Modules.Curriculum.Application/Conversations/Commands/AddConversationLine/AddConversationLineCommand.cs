using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.AddConversationLine;

public sealed record AddConversationLineCommand(
    Guid ConversationId,
    string Speaker,
    string Text,
    Dictionary<string, string>? Translations,
    string? AudioUrl) : ICommand;
