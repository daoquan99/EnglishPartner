using EnglishPartner.Modules.AI.Application.Abstractions;
using EnglishPartner.Modules.AI.Domain;
using EnglishPartner.Modules.AI.Domain.ChatSessions;
using EnglishPartner.Modules.AI.Domain.ChatSessions.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.AI.Application.Chat.Commands.SendMessage;

internal sealed class SendMessageCommandHandler(
    IChatSessionRepository sessionRepository,
    IGeminiService geminiService,
    ICurrentUserService currentUser) : ICommandHandler<SendMessageCommand, SendMessageResponse>
{
    public async Task<Result<SendMessageResponse>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;

        var session = await sessionRepository.GetByIdWithMessagesAsync(request.SessionId, cancellationToken);
        if (session is null || session.UserId != userId)
            return Result.Failure<SendMessageResponse>(AiErrors.ChatSession.NotFound);

        session.AddUserMessage(request.Text);

        var history = session.Messages
            .Select(m => new ChatTurn(m.Role == ChatRole.User ? "user" : "model", m.Text))
            .ToList();

        var modelReply = await geminiService.ChatAsync(session.SystemPrompt, history, request.Text, cancellationToken);

        var modelMessage = session.AddModelMessage(modelReply);
        sessionRepository.Update(session);

        return new SendMessageResponse(modelMessage.Id, modelReply);
    }
}
