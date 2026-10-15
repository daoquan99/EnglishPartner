using EnglishLiveTutor.Application;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Infrastructure;
using EnglishLiveTutor.Infrastructure.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnglishLiveTutor.Domain.Tests;

public class CqrsDependencyInjectionTests
{
    private static ServiceProvider CreateProvider(int? timeoutSeconds = null)
    {
        var settings = new Dictionary<string, string?>();
        if (timeoutSeconds.HasValue)
            settings["Ai:VocabularyGeneration:TimeoutSeconds"] = timeoutSeconds.Value.ToString();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<ICurrentUser>(new FakeCurrentUser());
        services.AddApplicationServices();
        services.AddInfrastructureServices(configuration);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    [Fact]
    public void NewEndpointHandlers_ShouldResolveTheirRepositoriesAndServices()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        Type[] handlerTypes =
        [
            typeof(EnglishLiveTutor.Application.Features.Vocabulary.Queries.BuildVocabularyGenerationPrompt.BuildVocabularyGenerationPromptQueryHandler),
            typeof(EnglishLiveTutor.Application.Features.Vocabulary.Commands.ImportVocabularyJson.ImportVocabularyJsonCommandHandler),
            typeof(EnglishLiveTutor.Application.Features.Vocabulary.Commands.GenerateVocabulary.GenerateVocabularyCommandHandler),
            typeof(EnglishLiveTutor.Application.Features.Vocabulary.Queries.ListVocabularyFlashcards.ListVocabularyFlashcardsQueryHandler),
            typeof(EnglishLiveTutor.Application.Features.Pronunciation.Queries.ListScoringRubrics.ListScoringRubricsQueryHandler),
            typeof(EnglishLiveTutor.Application.Features.AiModels.Queries.ListAiModelConfigs.ListAiModelConfigsQueryHandler),
            typeof(EnglishLiveTutor.Application.Features.Prompts.Queries.ListSpeechSpeeds.ListSpeechSpeedsQueryHandler),
            typeof(EnglishLiveTutor.Application.Features.Vocabulary.Commands.DeleteVocabulary.DeleteVocabularyCommandHandler),
            typeof(EnglishLiveTutor.Application.Features.Pronunciation.Commands.DeleteScoringRubric.DeleteScoringRubricCommandHandler),
            typeof(EnglishLiveTutor.Application.Features.AiModels.Commands.DeleteAiModelConfig.DeleteAiModelConfigCommandHandler),
            typeof(EnglishLiveTutor.Application.Features.Prompts.Commands.DeleteSpeechSpeed.DeleteSpeechSpeedCommandHandler),
            typeof(EnglishLiveTutor.Application.Features.Prompts.Commands.DeleteCefrLevelPrompt.DeleteCefrLevelPromptCommandHandler),
            typeof(EnglishLiveTutor.Application.Features.Prompts.Commands.DeleteConversationModePrompt.DeleteConversationModePromptCommandHandler),
            typeof(EnglishLiveTutor.Application.Features.Prompts.Commands.UpsertSpeechSpeed.UpsertSpeechSpeedCommandHandler),
            typeof(EnglishLiveTutor.Application.Features.Prompts.Commands.UpdateSpeechSpeed.UpdateSpeechSpeedCommandHandler),
            typeof(EnglishLiveTutor.Application.Features.Vocabulary.Commands.CreateVocabulary.CreateVocabularyCommandHandler),
            typeof(EnglishLiveTutor.Application.Features.Vocabulary.Commands.UpdateVocabulary.UpdateVocabularyCommandHandler),
            typeof(EnglishLiveTutor.Application.Features.AiModels.Commands.CreateAiModelConfig.CreateAiModelConfigCommandHandler),
            typeof(EnglishLiveTutor.Application.Features.AiModels.Commands.UpdateAiModelConfig.UpdateAiModelConfigCommandHandler),
            typeof(EnglishLiveTutor.Application.Features.Pronunciation.Commands.SynthesizePronunciationAudio.SynthesizePronunciationAudioCommandHandler),
        ];
        foreach (var handlerType in handlerTypes)
        {
            var contract = handlerType.GetInterfaces().Single(i => i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(MediatR.IRequestHandler<,>));
            var resolved = scope.ServiceProvider.GetRequiredService(contract);
            Assert.IsType(handlerType, resolved);
        }
    }

    [Theory]
    [InlineData(null, 300)]
    [InlineData(600, 600)]
    [InlineData(1, 1)]
    [InlineData(3600, 3600)]
    public void VocabularyHttpClient_ShouldUseItsConfiguredTimeout_WithoutChangingDefaultClient(int? configured, int expected)
    {
        using var provider = CreateProvider(configured);
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        using var vocabularyClient = factory.CreateClient(nameof(IVocabularyAiGenerator));
        Assert.Equal(TimeSpan.FromSeconds(expected), vocabularyClient.Timeout);
        using var defaultClient = factory.CreateClient();
        Assert.Equal(TimeSpan.FromSeconds(100), defaultClient.Timeout);
        using var scope = provider.CreateScope();
        Assert.IsType<GeminiVocabularyAiGenerator>(scope.ServiceProvider.GetRequiredService<IVocabularyAiGenerator>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(3601)]
    public void VocabularyHttpClient_ShouldRejectInvalidTimeout(int configured)
    {
        using var provider = CreateProvider(configured);
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        Assert.Throws<OptionsValidationException>(() => factory.CreateClient(nameof(IVocabularyAiGenerator)));
    }

    [Fact]
    public async Task CreateAiModelConfig_WhenModelCodeIsEmpty_ShouldThrowValidationException()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<MediatR.ISender>();

        var cmd = new EnglishLiveTutor.Application.Features.AiModels.Commands.CreateAiModelConfig.CreateAiModelConfigCommand(
            new EnglishLiveTutor.Application.Common.Models.UpsertAiModelConfigRequest("Display", ""));

        var ex = await Assert.ThrowsAsync<EnglishLiveTutor.Application.Common.Validation.ValidationException>(() => sender.Send(cmd));
        Assert.Contains("Mã Model (ModelCode) không được để trống.", ex.Message);
    }
}

