using EnglishLiveTutor.Application.Common.Interfaces;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
            ?? "Server=localhost;Database=LingoMint;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;";

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IPermissionChecker>(EnglishLiveTutor.Infrastructure.Security.AllowAllPermissionChecker.Instance);
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

        // Security & Token Services
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IJwtTokenService, HmacJwtTokenService>();

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
        services.AddHostedService<EnglishLiveTutor.Infrastructure.VocabularyGames.FallingWordRoundExpirationWorker>();

        services.AddOptions<LessonMediaStorageOptions>()
            .Bind(configuration.GetSection(LessonMediaStorageOptions.SectionName));
        services.AddSingleton<ILessonMediaStorage, FileLessonMediaStorage>();

        services.AddSingleton<ILessonPromptTemplateProvider, LessonPromptTemplateProvider>();
        services.AddSingleton<ILessonPromptBuilder, LessonPromptBuilder>();

        // AI & Realtime Services
        services.AddSingleton<ICefrPromptBuilder, CefrPromptBuilder>();
        services.AddHttpClient();
        services.AddSingleton<FastTextTranslatorFallback>();
        services.AddSingleton<GrammarCorrectionAnalyzerService>();
        services.AddSingleton<IVocabularyGenerationContent, VocabularyGenerationContent>();

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
        services.AddScoped<IPronunciationAiAssessor, GeminiPronunciationAiAssessor>();
        services.AddScoped<ILivePronunciationTtsSynthesizer, GeminiLivePronunciationTtsSynthesizer>();
        services.AddSingleton<IPronunciationSynthesisLock, PronunciationSynthesisLock>();
        services.AddSingleton<GeminiLiveConnectionManager>();
        services.AddSingleton<ILiveSessionService>(sp => sp.GetRequiredService<GeminiLiveConnectionManager>());

        // Lesson Services
        services.AddSingleton<IDictationScorer, Lessons.DictationScorer>();
        services.AddSingleton<IDeterministicExerciseGrader, Lessons.DeterministicExerciseGrader>();
        services.AddSingleton<ILessonSanitizer, Lessons.LessonSanitizer>();
        services.AddOptions<LessonGenerationOptions>()
            .Bind(configuration.GetSection(LessonGenerationOptions.SectionName));
        services.AddOptions<LessonAssessmentOptions>()
            .Bind(configuration.GetSection(LessonAssessmentOptions.SectionName));
        services.AddOptions<LessonRolePlayOptions>()
            .Bind(configuration.GetSection(LessonRolePlayOptions.SectionName));

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
