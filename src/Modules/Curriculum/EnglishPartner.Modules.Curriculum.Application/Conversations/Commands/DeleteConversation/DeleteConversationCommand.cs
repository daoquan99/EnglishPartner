using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.DeleteConversation;

public sealed record DeleteConversationCommand(Guid Id) : ICommand;
