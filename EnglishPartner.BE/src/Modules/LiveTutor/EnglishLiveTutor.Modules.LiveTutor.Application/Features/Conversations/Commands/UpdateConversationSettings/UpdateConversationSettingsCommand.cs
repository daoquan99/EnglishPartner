using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Conversations;

namespace EnglishLiveTutor.Application.Features.Conversations.Commands.UpdateConversationSettings;

public sealed record UpdateConversationSettingsCommand(
    string Id,
    UpdateConversationSettingsRequest Request
) : ICommand<ConversationEntity?>;
