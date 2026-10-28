using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Vocabulary.Queries.BuildVocabularyGenerationPrompt;
using EnglishLiveTutor.Application.Features.Vocabulary.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Commands.GenerateVocabulary;

public sealed class GenerateVocabularyCommandHandler : ICommandHandler<GenerateVocabularyCommand, List<VocabularyFlashcardDto>>
{
    public const string PromptVersion = VocabularyGenerationDefaults.PromptVersion;

    private readonly IAppDbContextFactory _dbFactory;
    private readonly IVocabularyAiGenerator _aiGenerator;
    private readonly IVocabularyPromptBuilder _promptBuilder;
    private readonly VocabularyGenerationWriter _writer;

    public GenerateVocabularyCommandHandler(
        IAppDbContextFactory dbFactory,
        IVocabularyAiGenerator aiGenerator,
        IVocabularyPromptBuilder promptBuilder,
        VocabularyGenerationWriter writer)
    {
        _dbFactory = dbFactory;
        _aiGenerator = aiGenerator;
        _promptBuilder = promptBuilder;
        _writer = writer;
    }

    public async Task<List<VocabularyFlashcardDto>> HandleAsync(GenerateVocabularyCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        string requestedModelCode = string.IsNullOrWhiteSpace(req.ModelCode) ? "gemini-3.8-flash" : req.ModelCode.Trim();

        bool isAgy = string.Equals(req.Provider, "agy", StringComparison.OrdinalIgnoreCase) ||
                     requestedModelCode.StartsWith("agy:", StringComparison.OrdinalIgnoreCase) ||
                     requestedModelCode.EndsWith("-medium", StringComparison.OrdinalIgnoreCase) ||
                     requestedModelCode.EndsWith("-low", StringComparison.OrdinalIgnoreCase) ||
                     requestedModelCode.EndsWith("-high", StringComparison.OrdinalIgnoreCase);

        if (!isAgy && string.IsNullOrWhiteSpace(req.ApiKey))
            throw new InvalidOperationException("Cần cung cấp API Key của Google Gemini hoặc chọn Antigravity CLI Local để sinh từ vựng AI.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var model = await db.AiModelCatalog.AsNoTracking().FirstOrDefaultAsync(m => m.ModelCode == requestedModelCode, ct);

        if (!isAgy)
        {
            if (model == null)
                throw new InvalidOperationException($"Model '{requestedModelCode}' không tồn tại trong AI Model Catalog.");
            if (!model.IsEnabled || !model.IsAvailable || !model.SupportsTextGeneration || model.SupportsLiveAudio)
                throw new InvalidOperationException($"Model '{model.DisplayName}' không hỗ trợ tạo từ vựng cấu trúc JSON.");
        }

        string effectiveModel = model != null ? model.GetEffectiveModelCode() : requestedModelCode;

        var prompt = await _promptBuilder.BuildPromptAsync(new(req.RequestedCount, req.CefrLevel, req.Topic, req.ExcludedWords), ct);

        var context = new VocabularyGenerationContext(command.UserId, requestedModelCode, prompt.RequestedCount,
            prompt.CefrLevel, prompt.Topic, req.AssignToTodayPlan);
        try
        {
            var generated = await _aiGenerator.GenerateVocabularyAsync(
                req.ApiKey?.Trim() ?? string.Empty,
                effectiveModel,
                prompt.CefrLevel,
                prompt.Topic,
                prompt.RequestedCount,
                prompt.ExcludedWords.ToList(),
                req.Provider,
                ct);
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
