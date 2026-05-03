using EnglishPartner.Modules.AI.Contracts.Services;
using EnglishPartner.Modules.Curriculum.Domain;
using EnglishPartner.Modules.Curriculum.Domain.Conversations.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.GenerateLineAudio;

internal sealed class GenerateLineAudioCommandHandler(
    IConversationRepository repository,
    IAiEvaluationService aiEvaluationService,
    IFileStorageService fileStorageService) : ICommandHandler<GenerateLineAudioCommand, GenerateLineAudioResponse>
{
    public async Task<Result<GenerateLineAudioResponse>> Handle(
        GenerateLineAudioCommand request, CancellationToken cancellationToken)
    {
        var conversation = await repository.GetByIdAsync(request.ConversationId, cancellationToken);
        if (conversation is null)
            return Result.Failure<GenerateLineAudioResponse>(CurriculumErrors.Conversation.NotFound(request.ConversationId));

        var line = conversation.Lines.FirstOrDefault(l => l.Id == request.LineId);
        if (line is null)
            return Result.Failure<GenerateLineAudioResponse>(CurriculumErrors.Conversation.LineNotFound(request.LineId));

        // Generate TTS audio
        var audioStream = await aiEvaluationService.GenerateTtsAsync(
            line.Text, conversation.LanguageCode, request.Voice, cancellationToken);

        if (audioStream is null)
            return Result.Failure<GenerateLineAudioResponse>(
                CommonErrors.Failure("Curriculum.TtsGenerationFailed", "Failed to generate TTS audio for this line."));

        // Upload to storage
        var fileName = $"{request.LineId:N}.mp3";
        var folder = $"conversations/{request.ConversationId:N}/audio";
        var relativePath = await fileStorageService.UploadAsync(audioStream, fileName, folder, cancellationToken);
        var publicUrl = fileStorageService.GetPublicUrl(relativePath);

        // Update domain entity
        conversation.UpdateLine(request.LineId, line.Speaker, line.Text, publicUrl);
        repository.Update(conversation);

        return new GenerateLineAudioResponse(publicUrl);
    }
}
