using EnglishPartner.Modules.AI.Contracts.Services;
using EnglishPartner.Modules.Curriculum.Domain;
using EnglishPartner.Modules.Curriculum.Domain.Conversations.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.GenerateAllLineAudio;

internal sealed class GenerateAllLineAudioCommandHandler(
    IConversationRepository repository,
    IAiEvaluationService aiEvaluationService,
    IFileStorageService fileStorageService,
    ILogger<GenerateAllLineAudioCommandHandler> logger) : ICommandHandler<GenerateAllLineAudioCommand, GenerateAllLineAudioResponse>
{
    public async Task<Result<GenerateAllLineAudioResponse>> Handle(
        GenerateAllLineAudioCommand request, CancellationToken cancellationToken)
    {
        var conversation = await repository.GetByIdAsync(request.ConversationId, cancellationToken);
        if (conversation is null)
            return Result.Failure<GenerateAllLineAudioResponse>(CurriculumErrors.Conversation.NotFound(request.ConversationId));

        var linesToGenerate = conversation.Lines
            .Where(l => string.IsNullOrEmpty(l.AudioUrl))
            .OrderBy(l => l.SortOrder)
            .ToList();

        var generatedLines = new List<GeneratedLineInfo>();
        var folder = $"conversations/{request.ConversationId:N}/audio";

        foreach (var line in linesToGenerate)
        {
            try
            {
                var audioStream = await aiEvaluationService.GenerateTtsAsync(
                    line.Text, conversation.LanguageCode, request.Voice, cancellationToken);

                if (audioStream is null)
                {
                    logger.LogWarning("TTS returned null for line {LineId}, skipping.", line.Id);
                    continue;
                }

                var fileName = $"{line.Id:N}.mp3";
                var relativePath = await fileStorageService.UploadAsync(audioStream, fileName, folder, cancellationToken);
                var publicUrl = fileStorageService.GetPublicUrl(relativePath);

                conversation.UpdateLine(line.Id, line.Speaker, line.Text, publicUrl);
                generatedLines.Add(new GeneratedLineInfo(line.Id, publicUrl));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to generate audio for line {LineId}, skipping.", line.Id);
            }
        }

        if (generatedLines.Count > 0)
            repository.Update(conversation);

        return new GenerateAllLineAudioResponse(
            TotalLines: conversation.Lines.Count,
            GeneratedCount: generatedLines.Count,
            SkippedCount: linesToGenerate.Count - generatedLines.Count,
            GeneratedLines: generatedLines);
    }
}
