using EnglishLiveTutor.Application.Features.Learning.Services;
using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Application.Features.Vocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishLiveTutor.Modules.Vocabulary;

public static class VocabularyModuleServiceCollectionExtensions
{
    public static IServiceCollection AddVocabularyModule(this IServiceCollection services)
    {
        services.AddScoped<IPronunciationTargetTextSource, PronunciationTargetTextSource>();
        services.AddScoped<IPronunciationAssessmentObserver, VocabularyPronunciationProgressObserver>();
        services.AddScoped<VocabularyGenerationWriter>();
        services.AddScoped<IVocabularyPromptBuilder, VocabularyPromptBuilder>();
        services.AddScoped<IDailyVocabularyPlanInitializer, DailyVocabularyPlanInitializer>();

        return services;
    }
}
