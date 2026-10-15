using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Conversations;
using EnglishLiveTutor.Domain.Conversations.Repositories;

namespace EnglishLiveTutor.Application.Features.Conversations.Commands.CreateConversation;

public sealed record CreateConversationCommand(CreateConversationRequest Request) : ICommand<ConversationEntity>;
