using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Conversations.Repositories;

namespace EnglishLiveTutor.Application.Features.Conversations.Commands.DeleteConversation;

public sealed record DeleteConversationCommand(string Id) : ICommand<bool>;
