using EnglishPartner.Modules.AI.Domain.ChatSessions;
using EnglishPartner.Modules.AI.Domain.ChatSessions.Repositories;
using EnglishPartner.Modules.Learning.Contracts.Services;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.AI.Application.Chat.Commands.StartChat;

internal sealed class StartChatCommandHandler(
    IChatSessionRepository sessionRepository,
    ILearningProfileQueryService learningProfileQueryService,
    ICurrentUserService currentUser) : ICommandHandler<StartChatCommand, Guid>
{
    public async Task<Result<Guid>> Handle(StartChatCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var systemPrompt = request.SystemPrompt;

        // If no system prompt provided by client, build a personalized one
        if (string.IsNullOrWhiteSpace(systemPrompt))
        {
            var profile = await learningProfileQueryService.GetUserLevelAsync(userId, cancellationToken);
            
            var level = profile?.CurrentLevel ?? "Beginner";
            
            systemPrompt = $"""
                You are a friendly, encouraging, and highly effective English language tutor.
                You are chatting with a student whose current English level is: {level}.
                
                Guidelines:
                1. Always adjust your vocabulary and grammar complexity to match a {level} student.
                2. If the student makes a mistake, gently correct them, explain why, and encourage them.
                3. Keep your responses concise and engaging (1-3 short paragraphs).
                4. Ask questions to keep the conversation flowing.
                5. Do not write essays or overwhelm the student with too much information at once.
                """;
        }

        var session = ChatSession.Start(userId, request.Title, systemPrompt);
        sessionRepository.Add(session);
        
        return session.Id;
    }
}
