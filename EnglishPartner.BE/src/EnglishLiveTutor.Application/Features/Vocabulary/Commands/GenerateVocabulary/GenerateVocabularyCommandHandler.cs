using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Vocabulary.Queries.BuildVocabularyGenerationPrompt;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Commands.GenerateVocabulary;

public sealed class GenerateVocabularyCommandHandler : ICommandHandler<GenerateVocabularyCommand, List<VocabularyFlashcardDto>>
{
    public const string PromptVersion = VocabularyGenerationDefaults.PromptVersion;

    private readonly IAppDbContextFactory _dbFactory;
    private readonly IVocabularyAiGenerator _aiGenerator;
    private readonly IQueryHandler<BuildVocabularyGenerationPromptQuery, VocabularyGenerationPromptDto> _promptHandler;
    private readonly VocabularyGenerationWriter _writer;

    public GenerateVocabularyCommandHandler(
        IAppDbContextFactory dbFactory,
        IVocabularyAiGenerator aiGenerator,
        IQueryHandler<BuildVocabularyGenerationPromptQuery, VocabularyGenerationPromptDto> promptHandler,
        VocabularyGenerationWriter writer)
    {
        _dbFactory = dbFactory;
        _aiGenerator = aiGenerator;
        _promptHandler = promptHandler;
        _writer = writer;
    }

    public async Task<List<VocabularyFlashcardDto>> HandleAsync(GenerateVocabularyCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.ApiKey))
            throw new InvalidOperationException("Cần cung cấp API Key của Google Gemini để sinh từ vựng AI.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        string requestedModelCode = string.IsNullOrWhiteSpace(req.ModelCode) ? "gemini-3.8-flash" : req.ModelCode.Trim();
        var model = await db.AiModelCatalog.AsNoTracking().FirstOrDefaultAsync(m => m.ModelCode == requestedModelCode, ct)
            ?? throw new InvalidOperationException($"Model '{requestedModelCode}' không tồn tại trong AI Model Catalog.");
        if (!model.IsEnabled || !model.IsAvailable || !model.SupportsTextGeneration || model.SupportsLiveAudio)
            throw new InvalidOperationException($"Model '{model.DisplayName}' không hỗ trợ tạo từ vựng cấu trúc JSON.");

        var query = new BuildVocabularyGenerationPromptQuery(new(req.RequestedCount, req.CefrLevel, req.Topic, req.ExcludedWords));
        var prompt = await _promptHandler.HandleAsync(query, ct);

        var context = new VocabularyGenerationContext(command.UserId, requestedModelCode, prompt.RequestedCount,
            prompt.CefrLevel, prompt.Topic, req.AssignToTodayPlan);
        try
        {
            var generated = await _aiGenerator.GenerateVocabularyAsync(req.ApiKey.Trim(), model.GetEffectiveModelCode(),
                prompt.CefrLevel, prompt.Topic, prompt.RequestedCount, prompt.ExcludedWords.ToList(), ct);
            return await _writer.SaveAsync(context, generated, ct);
        }
        catch (Exception ex)
        {
            await _writer.RecordFailureAsync(context, ex);
            throw;
        }
    }

    private IAppDbContextFactory dbFactory => _dbFactory;
}
