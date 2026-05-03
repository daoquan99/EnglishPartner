using EnglishPartner.SharedKernel.CQRS;
using Microsoft.AspNetCore.Http;

namespace EnglishPartner.Modules.Learning.Application.Conversations.Commands.EvaluateConversationLine;

public sealed record EvaluateConversationLineCommand(
    Guid ConversationId,
    Guid LineId,
    IFormFile AudioFile) : ICommand<EvaluateConversationLineResponse>;

public sealed record EvaluateConversationLineResponse(
    int Score, // 0-100
    string Transcription,
    string? PronunciationFeedback);
