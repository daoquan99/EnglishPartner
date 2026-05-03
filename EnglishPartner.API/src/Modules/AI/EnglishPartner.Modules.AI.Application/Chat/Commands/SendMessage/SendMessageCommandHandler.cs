using EnglishPartner.Modules.AI.Application.Abstractions;
using EnglishPartner.Modules.AI.Domain;
using EnglishPartner.Modules.AI.Domain.ChatSessions;
using EnglishPartner.Modules.AI.Domain.ChatSessions.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;
using EnglishPartner.SharedKernel.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishPartner.Modules.AI.Application.Chat.Commands.SendMessage;

internal sealed class SendMessageCommandHandler(
    IChatSessionRepository sessionRepository,
    IGeminiService geminiService,
    IOptions<GeminiAISettings> settings,
    IServiceScopeFactory scopeFactory,
    ILogger<SendMessageCommandHandler> logger,
    ICurrentUserService currentUser) : ICommandHandler<SendMessageCommand, SendMessageResponse>
{
    public async Task<Result<SendMessageResponse>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var config = settings.Value;

        var session = await sessionRepository.GetByIdWithMessagesAsync(request.SessionId, cancellationToken);
        if (session is null || session.UserId != userId)
            return Result.Failure<SendMessageResponse>(AiErrors.ChatSession.NotFound);

        session.AddUserMessage(request.Text);

        // Build context: latest summary + recent messages (sliding window)
        var latestSummary = await sessionRepository.GetLatestSummaryAsync(session.Id, cancellationToken);
        var summaryEndOrder = latestSummary?.EndOrder ?? 0;

        var recentMessages = session.Messages
            .Where(m => m.Order > summaryEndOrder)
            .OrderBy(m => m.Order)
            .TakeLast(config.MaxChatHistoryMessages)
            .ToList();

        var history = new List<ChatTurn>();

        // Prepend summary as context if available
        if (latestSummary is not null)
        {
            history.Add(new ChatTurn("user", $"[Previous conversation summary]\n{latestSummary.Summary}"));
            history.Add(new ChatTurn("model", "Understood. I'll keep this context in mind as we continue."));
        }

        foreach (var m in recentMessages)
        {
            history.Add(new ChatTurn(m.Role == ChatRole.User ? "user" : "model", m.Text));
        }

        var modelReply = await geminiService.ChatAsync(session.SystemPrompt, history, request.Text, cancellationToken);

        var modelMessage = session.AddModelMessage(modelReply);

        // Fire-and-forget: check if summarization is needed
        var totalMessages = session.Messages.Count;
        var nextSummarizationAt = summaryEndOrder + config.MaxChatHistoryMessages;
        if (totalMessages >= nextSummarizationAt)
        {
            var sessionId = session.Id;
            _ = Task.Run(async () =>
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IChatSessionRepository>();
                    var uow = scope.ServiceProvider.GetRequiredService<IAiUnitOfWork>();
                    var ai = scope.ServiceProvider.GetRequiredService<IGeminiService>();
                    var log = scope.ServiceProvider.GetRequiredService<ILogger<SendMessageCommandHandler>>();

                    var sess = await repo.GetByIdWithMessagesAsync(sessionId);
                    if (sess is null) return;

                    var prevSummary = await repo.GetLatestSummaryAsync(sessionId);
                    var prevEndOrder = prevSummary?.EndOrder ?? 0;
                    var batchSize = config.MaxChatHistoryMessages;

                    var batchMessages = sess.Messages
                        .Where(m => m.Order > prevEndOrder && m.Order <= prevEndOrder + batchSize)
                        .OrderBy(m => m.Order)
                        .Select(m => new ChatTurn(m.Role == ChatRole.User ? "user" : "model", m.Text))
                        .ToList();

                    if (batchMessages.Count == 0) return;

                    var summaryText = await ai.SummarizeHistoryAsync(prevSummary?.Summary, batchMessages);

                    var newStartOrder = prevEndOrder + 1;
                    var newEndOrder = prevEndOrder + batchMessages.Count;
                    sess.AddSummary(newStartOrder, newEndOrder, summaryText);
                    await uow.SaveChangesAsync();

                    log.LogInformation(
                        "Created history summary for session {SessionId}: orders {Start}-{End} ({Len} chars).",
                        sessionId, newStartOrder, newEndOrder, summaryText.Length);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Background summarization failed for session {SessionId}.", sessionId);
                }
            });
        }

        return new SendMessageResponse(modelMessage.Id, modelReply);
    }
}
