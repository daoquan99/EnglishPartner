using EnglishPartner.Modules.AI.Contracts.Services;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Learning.Application.Pronunciation.Commands.EvaluatePronunciation;

internal sealed class EvaluatePronunciationCommandHandler(
    IAiEvaluationService aiEvaluationService) : ICommandHandler<EvaluatePronunciationCommand, PronunciationEvaluationResponse>
{
    public async Task<Result<PronunciationEvaluationResponse>> Handle(
        EvaluatePronunciationCommand request,
        CancellationToken cancellationToken)
    {
        using var stream = request.AudioFile.OpenReadStream();
        var evaluation = await aiEvaluationService.EvaluatePronunciationAsync(
            targetText: request.ExpectedText,
            audioStream: stream,
            targetLanguage: request.LanguageCode ?? "en-US",
            mimeType: request.AudioFile.ContentType ?? "audio/webm",
            cancellationToken: cancellationToken);

        if (evaluation is null)
        {
            return Result.Failure<PronunciationEvaluationResponse>(
                new Error("AI.EvaluationFailed", "Failed to evaluate pronunciation. Please try again."));
        }

        return new PronunciationEvaluationResponse(
            evaluation.Score,
            evaluation.Transcription,
            evaluation.PronunciationFeedback);
    }
}
