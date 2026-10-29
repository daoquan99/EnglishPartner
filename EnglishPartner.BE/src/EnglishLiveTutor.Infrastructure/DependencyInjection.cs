using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Grammar.ContentPacks;
using EnglishLiveTutor.Application.Features.Grammar.Ports;
using EnglishLiveTutor.Infrastructure.Grammar;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Domain.AiCatalog.Repositories;
using EnglishLiveTutor.Domain.Conversations.Repositories;
using EnglishLiveTutor.Domain.Identity.Repositories;
using EnglishLiveTutor.Domain.Learning.Repositories;
using EnglishLiveTutor.Domain.Prompts.Repositories;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;
using EnglishLiveTutor.Domain.Vocabulary.Repositories;
using EnglishLiveTutor.Domain.Topics.Repositories;
using EnglishLiveTutor.Domain.Voices.Repositories;
using EnglishLiveTutor.Domain.Lessons.Repositories;
using EnglishLiveTutor.Infrastructure.Lessons.PromptBuilder;
using EnglishLiveTutor.Infrastructure.Storage;
using EnglishLiveTutor.Application.Features.Conversations.Ports;
using EnglishLiveTutor.Infrastructure.Ai;
using EnglishLiveTutor.Infrastructure.Identity;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Interceptors;
using EnglishLiveTutor.Infrastructure.Realtime;
using EnglishLiveTutor.Infrastructure.Repositories;
using EnglishLiveTutor.Infrastructure.VocabularyGames;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishLiveTutor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("Connection string is not configured. Please configure ConnectionStrings:DefaultConnection or ConnectionStrings:SqlServer.");

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ILiveSessionRealtimeNotifier>(NullLiveSessionRealtimeNotifier.Instance);
        services.AddSingleton<AuditingSaveChangesInterceptor>();

        services.AddDbContextFactory<AppDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            options.AddInterceptors(sp.GetRequiredService<AuditingSaveChangesInterceptor>());
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IAppDbContextFactory, AppDbContextFactoryWrapper>();
        services.AddSingleton<IGrammarContentPackSource, EmbeddedGrammarContentPackSource>();

        // Security & Token Services
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();
        services.TryAddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();

        services.AddOptions<AdminUserSeedOptions>()
            .Bind(configuration.GetSection(AdminUserSeedOptions.SectionName));
        services.AddOptions<DemoLearnerSeedOptions>()
            .Bind(configuration.GetSection(DemoLearnerSeedOptions.SectionName));

        services.AddSingleton<IJwtTokenService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<JwtOptions>>();
            var env = sp.GetService<IHostEnvironment>();
            var logger = sp.GetService<ILogger<HmacJwtTokenService>>();
            var timeProvider = sp.GetService<TimeProvider>();
            return new HmacJwtTokenService(options, env, logger, timeProvider);
        });

        // Aggregate Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IPromptConfigurationRepository, PromptConfigurationRepository>();
        services.AddScoped<ISpeechSpeedRepository, SpeechSpeedRepository>();
        services.AddScoped<ITeacherVoiceRepository, TeacherVoiceRepository>();
        services.AddScoped<ITopicRepository, TopicRepository>();
        services.AddSingleton<IConversationRepository, ConversationRepository>();
        services.AddScoped<IVocabularyRepository, VocabularyRepository>();
        services.AddScoped<IAiModelCatalogRepository, AiModelCatalogRepository>();
        services.AddScoped<IDailyVocabularyPlanRepository, DailyVocabularyPlanRepository>();
        services.AddScoped<IUserVocabularyProgressRepository, UserVocabularyProgressRepository>();
        services.AddScoped<IScoringRubricRepository, ScoringRubricRepository>();
        services.AddScoped<IPronunciationAssessmentRepository, PronunciationAssessmentRepository>();
        services.AddScoped<IGeneratedPronunciationAudioRepository, GeneratedPronunciationAudioRepository>();
        services.AddScoped<ILessonRolePlayRepository, LessonRolePlayRepository>();
        services.AddScoped<IAudioRecordingRepository, AudioRecordingRepository>();
        services.AddScoped<IIdempotencyService, IdempotencyService>();
        services.AddScoped<EnglishLiveTutor.Domain.VocabularyGames.Repositories.IFallingWordGameRepository, EnglishLiveTutor.Infrastructure.Repositories.FallingWordGameRepository>();
        services.AddScoped<EnglishLiveTutor.Application.Features.VocabularyGames.Ports.IFallingWordPoolReader, EnglishLiveTutor.Infrastructure.VocabularyGames.Queries.FallingWordPoolReader>();

        services.AddOptions<VocabularyGamesOptions>()
            .Bind(configuration.GetSection(VocabularyGamesOptions.SectionName))
            .Validate(options => options.ExpirationPollSeconds >= VocabularyGamesOptions.MinExpirationPollSeconds &&
                                 options.ExpirationPollSeconds <= VocabularyGamesOptions.MaxExpirationPollSeconds,
                $"VocabularyGames:ExpirationPollSeconds must be between {VocabularyGamesOptions.MinExpirationPollSeconds} and {VocabularyGamesOptions.MaxExpirationPollSeconds}.")
            .ValidateOnStart();

        services.AddHostedService<EnglishLiveTutor.Infrastructure.VocabularyGames.FallingWordRoundExpirationWorker>();

        services.AddOptions<LessonMediaStorageOptions>()
            .Bind(configuration.GetSection(LessonMediaStorageOptions.SectionName))
            .Validate(options => options.MaxFileSizeBytes > 0,
                "Storage:LessonMedia:MaxFileSizeBytes must be greater than 0.")
            .ValidateOnStart();
        services.AddSingleton<ILessonMediaStorage, FileLessonMediaStorage>();

        services.AddSingleton<ILessonPromptTemplateProvider, LessonPromptTemplateProvider>();
        services.AddSingleton<ILessonPromptBuilder, LessonPromptBuilder>();

        // AI & Realtime Services
        services.AddSingleton<ICefrPromptBuilder, CefrPromptBuilder>();
        services.AddHttpClient();
        services.AddSingleton<FastTextTranslatorFallback>();
        services.AddSingleton<GrammarCorrectionAnalyzerService>();
        services.AddSingleton<IVocabularyGenerationContent, VocabularyGenerationContent>();
        services.AddOptions<AntigravityCliOptions>()
            .Bind(configuration.GetSection(AntigravityCliOptions.SectionName))
            .Validate(options => options.TimeoutSeconds > 0,
                "Ai:AntigravityCli:TimeoutSeconds must be greater than 0.")
            .Validate(options => options.AllowedModels != null && options.AllowedModels.Count > 0,
                "Ai:AntigravityCli:AllowedModels must not be empty.")
            .ValidateOnStart();
        services.AddSingleton<IAgyCliRunner, AgyCliRunner>();

        services.AddOptions<VocabularyGenerationOptions>()
            .Bind(configuration.GetSection(VocabularyGenerationOptions.SectionName))
            .Validate(options => options.TimeoutSeconds > 0 && options.TimeoutSeconds <= 3600,
                "Ai:VocabularyGeneration:TimeoutSeconds must be between 1 and 3600.")
            .ValidateOnStart();

        services.AddHttpClient<IVocabularyAiGenerator, GeminiVocabularyAiGenerator>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<VocabularyGenerationOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddOptions<GrammarGenerationAiOptions>()
            .Bind(configuration.GetSection(GrammarGenerationAiOptions.SectionName))
            .Validate(options => options.TimeoutSeconds > 0 && options.TimeoutSeconds <= 3600,
                "Ai:GrammarGeneration:TimeoutSeconds must be between 1 and 3600.")
            .ValidateOnStart();

        services.AddHttpClient<IGrammarExerciseAiGenerator, GeminiGrammarExerciseAiGenerator>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<GrammarGenerationAiOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddScoped<IPronunciationAiAssessor, GeminiPronunciationAiAssessor>();
        services.AddScoped<ILivePronunciationTtsSynthesizer, GeminiLivePronunciationTtsSynthesizer>();
        services.AddSingleton<IPronunciationSynthesisLock, PronunciationSynthesisLock>();
        services.AddOptions<LiveSessionOptions>()
            .Bind(configuration.GetSection(LiveSessionOptions.SectionName));
        services.AddSingleton<GeminiLiveConnectionManager>();
        services.AddSingleton<ILiveSessionService>(sp => sp.GetRequiredService<GeminiLiveConnectionManager>());

        // Lesson Services
        services.AddSingleton<IDictationScorer, Lessons.DictationScorer>();
        services.AddSingleton<IDeterministicExerciseGrader, Lessons.DeterministicExerciseGrader>();
        services.AddSingleton<ILessonSanitizer, Lessons.LessonSanitizer>();
        services.AddOptions<LessonGenerationOptions>()
            .Bind(configuration.GetSection(LessonGenerationOptions.SectionName))
            .Validate(options => options.TimeoutSeconds is >= 1 and <= 600,
                "Ai:LessonGeneration:TimeoutSeconds must be between 1 and 600.")
            .Validate(options => options.MaxRetries is >= 0 and <= 5,
                "Ai:LessonGeneration:MaxRetries must be between 0 and 5.")
            .ValidateOnStart();
        services.AddOptions<LessonAssessmentOptions>()
            .Bind(configuration.GetSection(LessonAssessmentOptions.SectionName))
            .Validate(options => options.TimeoutSeconds is >= 1 and <= 600,
                "Ai:LessonAssessment:TimeoutSeconds must be between 1 and 600.")
            .Validate(options => options.MaxRetries is >= 0 and <= 5,
                "Ai:LessonAssessment:MaxRetries must be between 0 and 5.")
            .ValidateOnStart();
        services.AddOptions<LessonRolePlayOptions>()
            .Bind(configuration.GetSection(LessonRolePlayOptions.SectionName))
            .Validate(options => options.TimeoutSeconds is >= 1 and <= 600,
                "Ai:LessonRolePlay:TimeoutSeconds must be between 1 and 600.")
            .Validate(options => options.MaxRetries is >= 0 and <= 5,
                "Ai:LessonRolePlay:MaxRetries must be between 0 and 5.")
            .ValidateOnStart();

        services.AddHttpClient<ILessonAiGenerator, GeminiLessonAiGenerator>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<LessonGenerationOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddHttpClient<IRolePlayReplyProvider, GeminiRolePlayReplyProvider>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<LessonRolePlayOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddHttpClient<ILessonAiGrader, Lessons.LessonAiGrader>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<LessonAssessmentOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddHttpClient<IDictationAiReviewer, Lessons.DictationAiReviewer>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<LessonAssessmentOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        return services;
    }
}
