using EnglishPartner.Modules.AI.Application.Abstractions;
using EnglishPartner.Modules.AI.Domain;
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
                    AiErrors.EvaluateAudio.ItemNotFound(request.ItemId, request.ItemType));

            itemContext = BuildContextString(contextDto);
        }
        else if (!string.IsNullOrWhiteSpace(request.ItemContext))
        {
            itemContext = request.ItemContext;
        }
        else
        {
            return Result.Failure<EvaluateAudioResponse>(AiErrors.EvaluateAudio.MissingContext);
        }

        // 2. Read audio stream into byte array for Gemini
        byte[] audioBytes;
        try
        {
            using var memoryStream = new MemoryStream();
            await request.AudioStream.CopyToAsync(memoryStream, cancellationToken);
            audioBytes = memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read audio stream.");
            return Result.Failure<EvaluateAudioResponse>(AiErrors.EvaluateAudio.ReadFailed);
        }

        // 3. Upload file for permanent storage using a new stream from the already-read bytes
        // (original AudioStream from IFormFile is not seekable — cannot rewind)
        string relativePath;
        try
        {
            using var uploadStream = new MemoryStream(audioBytes);
            relativePath = await fileStorageService.UploadAsync(uploadStream, request.FileName, "audio-submissions", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload audio file to storage.");
            return Result.Failure<EvaluateAudioResponse>(AiErrors.EvaluateAudio.UploadFailed);
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
