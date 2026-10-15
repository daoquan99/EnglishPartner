using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.AiCatalog.Repositories;
using EnglishLiveTutor.Domain.Conversations.Repositories;
using EnglishLiveTutor.Domain.Identity.Repositories;
using EnglishLiveTutor.Domain.Learning.Repositories;
using EnglishLiveTutor.Domain.Prompts.Repositories;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;
using EnglishLiveTutor.Domain.Vocabulary.Repositories;
using EnglishLiveTutor.Domain.Topics.Repositories;
using EnglishLiveTutor.Domain.Voices.Repositories;
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
        services.TryAddSingleton<ICurrentUser>(SystemCurrentUser.Instance);
        services.AddSingleton<AuditingSaveChangesInterceptor>();

        services.AddDbContextFactory<AppDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString);
            options.AddInterceptors(sp.GetRequiredService<AuditingSaveChangesInterceptor>());
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddSingleton<IAppDbContextFactory, AppDbContextFactoryWrapper>();

        // Security & Token Services
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IJwtTokenService, HmacJwtTokenService>();

        // Aggregate Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IPromptConfigurationRepository, PromptConfigurationRepository>();
        services.AddScoped<ITeacherVoiceRepository, TeacherVoiceRepository>();
        services.AddScoped<ITopicRepository, TopicRepository>();
        services.AddSingleton<IConversationRepository, ConversationRepository>();
        services.AddScoped<IVocabularyRepository, VocabularyRepository>();
        services.AddScoped<IAiModelCatalogRepository, AiModelCatalogRepository>();
        services.AddScoped<IDailyVocabularyPlanRepository, DailyVocabularyPlanRepository>();
        services.AddScoped<IUserVocabularyProgressRepository, UserVocabularyProgressRepository>();
        services.AddScoped<IScoringRubricRepository, ScoringRubricRepository>();
        services.AddScoped<IPronunciationAssessmentRepository, PronunciationAssessmentRepository>();

        // AI & Realtime Services
        services.AddSingleton<ICefrPromptBuilder, CefrPromptBuilder>();
        services.AddHttpClient();
        services.AddSingleton<FastTextTranslatorFallback>();
        services.AddSingleton<GrammarCorrectionAnalyzerService>();
        services.AddScoped<IVocabularyAiGenerator, GeminiVocabularyAiGenerator>();
        services.AddScoped<IPronunciationAiAssessor, GeminiPronunciationAiAssessor>();
        services.AddSingleton<GeminiLiveConnectionManager>();

        return services;
    }
}
