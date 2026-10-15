using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Commands.ImportVocabularyJson;

public sealed class ImportVocabularyJsonCommandHandler(IVocabularyGenerationContent content, VocabularyGenerationWriter writer)
    : ICommandHandler<ImportVocabularyJsonCommand, List<VocabularyFlashcardDto>>
{
    public async Task<List<VocabularyFlashcardDto>> HandleAsync(ImportVocabularyJsonCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        string modelCode = string.IsNullOrWhiteSpace(req.ModelCode) ? "manual-web-ai" : req.ModelCode.Trim();
        if (modelCode.Length > 64)
            throw new InvalidOperationException("Tên model không được dài quá 64 ký tự.");

        var context = new VocabularyGenerationContext(command.UserId, modelCode, 0,
            VocabularyGenerationDefaults.NormalizeCefr(req.CefrLevel),
            VocabularyGenerationDefaults.NormalizeTopic(req.Topic), req.AssignToTodayPlan);
        try
        {
            ct.ThrowIfCancellationRequested();
            var generated = content.ParseResponse(req.JsonText, context.CefrLevel);
            context = context with { RequestedCount = generated.Count };
            return await writer.SaveAsync(context, generated, ct);
        }
        catch (Exception ex)
        {
            await writer.RecordFailureAsync(context, ex);
            throw;
        }
    }
}
