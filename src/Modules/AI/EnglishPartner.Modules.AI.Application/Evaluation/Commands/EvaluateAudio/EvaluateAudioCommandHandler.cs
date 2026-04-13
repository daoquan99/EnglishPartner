using EnglishPartner.Modules.AI.Application.Abstractions;
using EnglishPartner.Modules.Curriculum.Contracts.Services;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace EnglishPartner.Modules.AI.Application.Evaluation.Commands.EvaluateAudio;

internal sealed class EvaluateAudioCommandHandler(
    IGeminiService geminiService,
    ICurriculumQueryService curriculumQueryService,
    IFileStorageService fileStorageService,
    ILogger<EvaluateAudioCommandHandler> logger) : ICommandHandler<EvaluateAudioCommand, EvaluateAudioResponse>
{
    public async Task<Result<EvaluateAudioResponse>> Handle(EvaluateAudioCommand request, CancellationToken cancellationToken)
    {
        // 1. Resolve Curriculum Context
        string itemContext;
        if (request.ItemId.HasValue && request.ItemType.HasValue)
        {
            var contextDto = await curriculumQueryService.GetItemContextAsync(
                request.ItemId.Value, request.ItemType.Value, cancellationToken);

            if (contextDto is null)
                return Result.Failure<EvaluateAudioResponse>(
                    new Error("AI.EvaluateAudio.ItemNotFound", $"Curriculum item {request.ItemId} of type {request.ItemType} not found."));

            itemContext = BuildContextString(contextDto);
        }
        else if (!string.IsNullOrWhiteSpace(request.ItemContext))
        {
            itemContext = request.ItemContext;
        }
        else
        {
            return Result.Failure<EvaluateAudioResponse>(
                new Error("AI.EvaluateAudio.MissingContext",
                    "Provide either (ItemId + ItemType) or ItemContext to evaluate."));
        }

        // 2. Read audio stream into byte array for Gemini
        byte[] audioBytes;
        try
        {
            using var memoryStream = new MemoryStream();
            await request.AudioStream.CopyToAsync(memoryStream, cancellationToken);
            audioBytes = memoryStream.ToArray();
            
            // Re-position stream to beginning for upload later
            request.AudioStream.Position = 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read audio stream.");
            return Result.Failure<EvaluateAudioResponse>(
                new Error("AI.EvaluateAudio.ReadFailed", "Could not read the uploaded audio file."));
        }

        // 3. Upload file for permanent storage
        string relativePath;
        try
        {
            relativePath = await fileStorageService.UploadAsync(request.AudioStream, request.FileName, "audio-submissions", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload audio file to storage.");
            return Result.Failure<EvaluateAudioResponse>(
                new Error("AI.EvaluateAudio.UploadFailed", "Could not save the uploaded audio file."));
        }
        
        var publicUrl = fileStorageService.GetPublicUrl(relativePath);

        // 4. Send to Gemini for Evaluation
        var result = await geminiService.EvaluateAudioAsync(itemContext, audioBytes, request.ContentType, cancellationToken);

        // 5. Construct and return response
        return new EvaluateAudioResponse(
            publicUrl,
            result.Transcription,
            result.QualityScore,
            result.IsCorrect,
            result.SuggestedCorrection,
            result.Explanation);
    }

    private static string BuildContextString(CurriculumItemContextDto dto) =>
        $"[{dto.ItemType}] {dto.Question}" +
        (dto.Hint is not null ? $"\nHint: {dto.Hint}" : "");
}
