using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.CreateConversation;

public sealed record CreateConversationCommand(
    string Title,
    string LanguageCode,
    string? Scenario,
    string? Level,
    string? ImageUrl,
    Guid? TopicId,
    Guid? GrammarId) : ICommand<Guid>;
