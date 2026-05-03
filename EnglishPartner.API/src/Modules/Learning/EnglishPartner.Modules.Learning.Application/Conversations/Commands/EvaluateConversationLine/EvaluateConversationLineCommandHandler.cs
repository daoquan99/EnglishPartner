using EnglishPartner.Modules.AI.Contracts.Services;
using EnglishPartner.Modules.Curriculum.Contracts.Services;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Learning.Application.Conversations.Commands.EvaluateConversationLine;

internal sealed class EvaluateConversationLineCommandHandler(
    IAiEvaluationService aiEvaluationService,
    ICurriculumQueryService curriculumQueryService) : ICommandHandler<EvaluateConversationLineCommand, EvaluateConversationLineResponse>
{
    public async Task<Result<EvaluateConversationLineResponse>> Handle(EvaluateConversationLineCommand request, CancellationToken cancellationToken)
    {
        var context = await curriculumQueryService.GetItemContextAsync(
            request.LineId, CurriculumItemType.ConversationLine, cancellationToken);

        if (context is null)
        {
            return Result.Failure<EvaluateConversationLineResponse>(CommonErrors.NotFound("ConversationLine", request.LineId));
        }

        // The exact line text is stored in CorrectAnswer, while Question has extra instructions.
        var targetText = context.CorrectAnswer;

        using var stream = request.AudioFile.OpenReadStream();
        var evaluation = await aiEvaluationService.EvaluatePronunciationAsync(
            targetText,
            stream,
            context.LanguageCode,
            request.AudioFile.ContentType ?? "audio/webm",
            cancellationToken);

        if (evaluation is null)
        {
            return Result.Failure<EvaluateConversationLineResponse>(
                new Error("AI.EvaluationFailed", "Failed to evaluate pronunciation. Please try again."));
        }

        return new EvaluateConversationLineResponse(
            evaluation.Score,
            evaluation.Transcription,
            evaluation.PronunciationFeedback);
    }
}
