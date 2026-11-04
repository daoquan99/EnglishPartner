using EnglishLiveTutor.Api.Configuration;
using EnglishLiveTutor.Application;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Prompts.Contracts;
using EnglishLiveTutor.Application.Features.Prompts.Services;
using EnglishLiveTutor.Infrastructure;
using EnglishLiveTutor.Infrastructure.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnglishLiveTutor.Domain.Tests;

public class CqrsDependencyInjectionTests
{
    private static ServiceProvider CreateProvider(
        int? timeoutSeconds = null,
        IPermissionChecker? permissionChecker = null,
        IDictionary<string, string?>? extraSettings = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=TestDb;Trusted_Connection=True;",
            ["Jwt:SecretKey"] = "test-jwt-secret-key-min-32-characters-for-testing"
        };
        if (timeoutSeconds.HasValue)
            settings["Ai:VocabularyGeneration:TimeoutSeconds"] = timeoutSeconds.Value.ToString();
        foreach (var (key, value) in extraSettings ?? new Dictionary<string, string?>())
            settings[key] = value;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<ICurrentUser>(new FakeCurrentUser());
        services.AddSingleton<IPermissionChecker>(permissionChecker ?? EnglishLiveTutor.Domain.Tests.TestDoubles.AllowAllPermissionChecker.Instance);
        services.AddApplicationServices();
        services.AddInfrastructureServices(configuration);
        services.AddSingleton<ILessonAiGenerator, TestLessonAiGenerator>();
        services.AddSingleton<ILessonAiGrader, FakeLessonAiGrader>();
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    private sealed class DenyPermissionChecker : IPermissionChecker
    {
        public Task<bool> HasPermissionAsync(string permission, CancellationToken cancellationToken = default)
            => Task.FromResult(false);
    }

    private sealed class TestLessonAiGenerator : ILessonAiGenerator
    {
        public Task<string> GenerateLessonExercisesJsonAsync(string promptText, string? clientApiKey, string? modelCode, string? provider = null, CancellationToken ct = default)
            => Task.FromResult("[]");
    }

    [Fact]
    public void Services_ShouldNotRegisterCustomCqrsHandlers_ForDirectInjection()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();

        var directCustomCqrsHandlers = services.Where(s => s.ServiceType.IsGenericType &&
            (s.ServiceType.GetGenericTypeDefinition() == typeof(IQueryHandler<,>) ||
             s.ServiceType.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) ||
             s.ServiceType.GetGenericTypeDefinition() == typeof(ICommandHandler<>)))
            .ToList();

        Assert.Empty(directCustomCqrsHandlers);
    }

    [Fact]
    public void PromptCatalogReader_ShouldBeRegisteredAsSingletonInApplication()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();

        var descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(IPromptCatalogReader));

        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(PromptCatalogReader), descriptor.ImplementationType);
    }

    [Fact]
    public void AllApplicationRequests_ShouldBeResolvableThroughMediatR()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var requestTypes = ApplicationAssemblies.All
            .SelectMany(assembly => assembly.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(i =>
                i == typeof(MediatR.IRequest) ||
                (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IRequest<>))))
            .ToList();

        Assert.NotEmpty(requestTypes);

        foreach (var reqType in requestTypes)
        {
            var reqInterface = reqType.GetInterfaces()
                .First(i => i == typeof(MediatR.IRequest) ||
                            (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IRequest<>)));

            Type handlerServiceType;
            if (reqInterface.IsGenericType)
            {
                var responseType = reqInterface.GetGenericArguments()[0];
                handlerServiceType = typeof(MediatR.IRequestHandler<,>).MakeGenericType(reqType, responseType);
            }
            else
            {
                handlerServiceType = typeof(MediatR.IRequestHandler<>).MakeGenericType(reqType);
            }

            var handler = scope.ServiceProvider.GetService(handlerServiceType);
            Assert.NotNull(handler);
        }
    }

    [Fact]
    public async Task RequestPipeline_ShouldPassThroughAuthorizationBehavior_AndThrowForbiddenException_WhenPermissionDenied()
    {
        using var provider = CreateProvider(permissionChecker: new DenyPermissionChecker());
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<MediatR.ISender>();

        var cmd = new EnglishLiveTutor.Application.Features.AiModels.Commands.CreateAiModelConfig.CreateAiModelConfigCommand(
            new EnglishLiveTutor.Application.Common.Models.UpsertAiModelConfigRequest("Display", "valid-code"));

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => sender.Send(cmd));
        Assert.Contains("rubrics.manage", ex.Message);
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
    [InlineData(null, 600)]
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

    [Theory]
    [InlineData(null, 600)]
    [InlineData(900, 900)]
    public void PronunciationAssessmentHttpClient_UsesConfiguredTimeout(int? configured, int expected)
    {
        var extra = new Dictionary<string, string?>();
        if (configured.HasValue)
            extra["Ai:PronunciationAssessment:TimeoutSeconds"] = configured.Value.ToString();
        using var provider = CreateProvider(extraSettings: extra);

        using var client = provider.GetRequiredService<IHttpClientFactory>()
            .CreateClient(nameof(IPronunciationAiAssessor));

        Assert.Equal(TimeSpan.FromSeconds(expected), client.Timeout);
    }

    [Fact]
    public void AiTimeoutOptions_DefaultsMatchAppsettingsContract()
    {
        using var provider = CreateProvider();

        Assert.Equal(600, provider.GetRequiredService<IOptions<EnglishLiveTutor.Infrastructure.Ai.PronunciationTtsOptions>>().Value.TimeoutSeconds);
        var live = provider.GetRequiredService<IOptions<EnglishLiveTutor.Infrastructure.Ai.LiveTutorAiOptions>>().Value;
        Assert.Equal(8, live.TranslationFallbackTimeoutSeconds);
        Assert.Equal(20, live.GrammarCorrectionTimeoutSeconds);
    }

    [Theory]
    [InlineData("Ai:PronunciationAssessment:TimeoutSeconds", "0")]
    [InlineData("Ai:PronunciationTts:TimeoutSeconds", "3601")]
    [InlineData("Ai:LiveTutor:TranslationFallbackTimeoutSeconds", "0")]
    [InlineData("Ai:LiveTutor:GrammarCorrectionTimeoutSeconds", "3601")]
    [InlineData("Ai:AntigravityCli:TimeoutSeconds", "3601")]
    public void AiTimeoutOptions_RejectOutOfRangeValues(string key, string value)
    {
        using var provider = CreateProvider(extraSettings: new Dictionary<string, string?> { [key] = value });

        Assert.Throws<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<IOptions<EnglishLiveTutor.Infrastructure.Ai.PronunciationAssessmentOptions>>().Value;
            _ = provider.GetRequiredService<IOptions<EnglishLiveTutor.Infrastructure.Ai.PronunciationTtsOptions>>().Value;
            _ = provider.GetRequiredService<IOptions<EnglishLiveTutor.Infrastructure.Ai.LiveTutorAiOptions>>().Value;
            _ = provider.GetRequiredService<IOptions<AntigravityCliOptions>>().Value;
        });
    }

    [Fact]
    public void LiveTutorHelpers_TakeTimeoutsFromOptions()
    {
        using var provider = CreateProvider(extraSettings: new Dictionary<string, string?>
        {
            ["Ai:LiveTutor:TranslationFallbackTimeoutSeconds"] = "30",
            ["Ai:LiveTutor:GrammarCorrectionTimeoutSeconds"] = "40",
        });

        var translator = provider.GetRequiredService<FastTextTranslatorFallback>();
        var analyzer = provider.GetRequiredService<GrammarCorrectionAnalyzerService>();

        Assert.Equal(TimeSpan.FromSeconds(30), GetHttpClient(translator).Timeout);
        Assert.Equal(TimeSpan.FromSeconds(40), GetHttpClient(analyzer).Timeout);

        static HttpClient GetHttpClient(object service) =>
            (HttpClient)service.GetType()
                .GetField("_http", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(service)!;
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

    [Fact]
    public void AddAppSignalR_WithoutRedisConfig_ShouldNotRegisterRedisServices()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Redis:ConnectionString"] = ""
        }).Build();

        services.AddAppSignalR(configuration, isDevelopment: true);

        var hasRedis = services.Any(s =>
            (s.ImplementationType?.FullName?.Contains("Redis", StringComparison.OrdinalIgnoreCase) ?? false) ||
            (s.ServiceType.FullName?.Contains("Redis", StringComparison.OrdinalIgnoreCase) ?? false));

        Assert.False(hasRedis);
    }

    [Fact]
    public void AddAppSignalR_WithRedisConfig_ShouldRegisterRedisSignalRServices()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Redis:ConnectionString"] = "localhost:6379"
        }).Build();

        services.AddAppSignalR(configuration, isDevelopment: true);

        var hasRedis = services.Any(s =>
            (s.ImplementationType?.FullName?.Contains("Redis", StringComparison.OrdinalIgnoreCase) ?? false) ||
            (s.ServiceType.FullName?.Contains("Redis", StringComparison.OrdinalIgnoreCase) ?? false));

        Assert.True(hasRedis);
    }
}
