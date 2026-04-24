using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.ReorderConversationLines;

public sealed record ReorderConversationLinesCommand(
    Guid ConversationId,
    List<Guid> OrderedLineIds) : ICommand;
