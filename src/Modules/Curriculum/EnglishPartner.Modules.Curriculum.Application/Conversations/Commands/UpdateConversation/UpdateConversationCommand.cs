using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.UpdateConversation;

public sealed record UpdateConversationCommand(
    Guid Id,
    string Title,
    string? Scenario,
    string? Level,
    string? ImageUrl,
    Guid? TopicId,
    Guid? GrammarId) : ICommand;
