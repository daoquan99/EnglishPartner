using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.AiModels.Contracts;
using EnglishLiveTutor.Application.Features.Vocabulary.Queries.BuildVocabularyGenerationPrompt;
using EnglishLiveTutor.Application.Features.Vocabulary.Services;
using MediatR;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Commands.GenerateVocabulary;

public sealed class GenerateVocabularyCommandHandler : ICommandHandler<GenerateVocabularyCommand, List<VocabularyFlashcardDto>>
{
    public const string PromptVersion = VocabularyGenerationDefaults.PromptVersion;

    private readonly IAiModelCatalogReader _modelCatalogReader;
    private readonly IVocabularyAiGenerator _aiGenerator;
    private readonly IVocabularyPromptBuilder _promptBuilder;
    private readonly VocabularyGenerationWriter _writer;
    private readonly IAgyCliRunner _agyRunner;

    public GenerateVocabularyCommandHandler(
        IVocabularyAiGenerator aiGenerator,
        IVocabularyPromptBuilder promptBuilder,
        VocabularyGenerationWriter writer,
        IAiModelCatalogReader modelCatalogReader,
        IAgyCliRunner agyRunner)
    {
        _modelCatalogReader = modelCatalogReader;
        _aiGenerator = aiGenerator;
        _promptBuilder = promptBuilder;
        _writer = writer;
        _agyRunner = agyRunner;
    }

    public async Task<List<VocabularyFlashcardDto>> HandleAsync(GenerateVocabularyCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        string requestedModelCode = string.IsNullOrWhiteSpace(req.ModelCode) ? "gemini-3.8-flash" : req.ModelCode.Trim();

        bool isAgy = _agyRunner.IsAgyRequest(req.Provider, requestedModelCode);
        if (isAgy)
        {
            _agyRunner.EnsureAgyEnabled();
        }

        if (!isAgy && string.IsNullOrWhiteSpace(req.ApiKey))
            throw new InvalidOperationException("Cần cung cấp API Key của Google Gemini hoặc chọn Antigravity CLI Local để sinh từ vựng AI.");

        var model = await _modelCatalogReader.FindByModelCodeAsync(requestedModelCode, ct);

        if (!isAgy)
        {
            if (model == null)
                throw new InvalidOperationException($"Model '{requestedModelCode}' không tồn tại trong AI Model Catalog.");
            if (!model.IsEnabled || !model.IsAvailable || !model.SupportsTextGeneration || model.SupportsLiveAudio)
                throw new InvalidOperationException($"Model '{model.DisplayName}' không hỗ trợ tạo từ vựng cấu trúc JSON.");
        }

        string effectiveModel = model?.EffectiveModelCode ?? requestedModelCode;

        var cleanTargetTerms = TargetedVocabularyHelper.CleanTargetTerms(req.TargetTerms);
        string? cleanSourceSentence = string.IsNullOrWhiteSpace(req.SourceSentence) ? null : req.SourceSentence.Trim();

        var promptReq = new BuildVocabularyGenerationPromptRequest(
            RequestedCount: req.RequestedCount,
            CefrLevel: req.CefrLevel,
            Topic: req.Topic,
            ExcludedWords: req.ExcludedWords,
            ExamplesPerSense: req.ExamplesPerSense,
            TargetTerms: cleanTargetTerms.Count > 0 ? cleanTargetTerms : null,
            SourceSentence: cleanSourceSentence);

        var prompt = await _promptBuilder.BuildPromptAsync(promptReq, ct);

        var context = new VocabularyGenerationContext(
            UserId: command.UserId,
            ModelCode: requestedModelCode,
            RequestedCount: prompt.RequestedCount,
            CefrLevel: prompt.CefrLevel,
            Topic: prompt.Topic,
            AssignToTodayPlan: req.AssignToTodayPlan,
            ExamplesPerSense: req.ExamplesPerSense,
            TargetTerms: cleanTargetTerms.Count > 0 ? cleanTargetTerms : null,
            SourceSentence: cleanSourceSentence);

        var options = new VocabularyGenerationRequestOptions(
            ApiKey: req.ApiKey?.Trim() ?? string.Empty,
            ModelCode: effectiveModel,
            CefrLevel: prompt.CefrLevel,
            Topic: prompt.Topic,
            Count: prompt.RequestedCount,
            ExcludedWords: prompt.ExcludedWords.ToList(),
            Provider: req.Provider,
            ExamplesPerSense: req.ExamplesPerSense,
            TargetTerms: cleanTargetTerms.Count > 0 ? cleanTargetTerms : null,
            SourceSentence: cleanSourceSentence);

        try
        {
            var generated = await _aiGenerator.GenerateVocabularyAsync(options, ct);
            return await _writer.SaveAsync(context, generated, ct);
        }
        catch (Exception ex)
        {
            await _writer.RecordFailureAsync(context, ex);
            throw;
        }
    }
}
