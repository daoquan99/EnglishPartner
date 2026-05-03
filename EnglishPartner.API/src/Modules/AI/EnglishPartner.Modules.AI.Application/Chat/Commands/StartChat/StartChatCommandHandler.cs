using EnglishPartner.Modules.AI.Domain.ChatSessions;
using EnglishPartner.Modules.AI.Domain.ChatSessions.Repositories;
using EnglishPartner.Modules.Learning.Contracts.Services;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;
using EnglishPartner.SharedKernel.Settings;
using Microsoft.Extensions.Options;

namespace EnglishPartner.Modules.AI.Application.Chat.Commands.StartChat;

internal sealed class StartChatCommandHandler(
    IChatSessionRepository sessionRepository,
    ILearningProfileQueryService learningProfileQueryService,
    IOptions<GeminiAISettings> settings,
    ICurrentUserService currentUser) 
    : ICommandHandler<StartChatCommand, Guid>
{
    public async Task<Result<Guid>> Handle(StartChatCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var systemPrompt = request.SystemPrompt;

        if (string.IsNullOrWhiteSpace(systemPrompt))
        {
            var profile = await learningProfileQueryService.GetUserLevelAsync(userId, cancellationToken);
            var level = profile?.CurrentLevel ?? "Beginner";
            
            var topicInstruction = !string.IsNullOrWhiteSpace(request.Topic)
                ? $"\nThe topic of the conversation is: {request.Topic}."
                : string.Empty;

            systemPrompt = settings.Value.DefaultChatSystemPrompt
                .Replace("{level}", level)
                .Replace("{topicInstruction}", topicInstruction);
        }

        var session = ChatSession.Start(userId, request.Title, systemPrompt, request.Topic);
        sessionRepository.Add(session);
        return session.Id;
    }
}
