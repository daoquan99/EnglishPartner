using EnglishLiveTutor.Application.Features.Pronunciation;
using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Application.Features.Pronunciation.Rubrics.Services;
using EnglishLiveTutor.Application.Features.Pronunciation.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishLiveTutor.Modules.Speech;

public static class SpeechModuleServiceCollectionExtensions
{
    public static IServiceCollection AddSpeechModule(this IServiceCollection services)
    {
        services.AddScoped<IPronunciationScoreReader, PronunciationScoreReader>();
        services.AddScoped<IAudioRecordingStore, AudioRecordingStore>();
        services.AddScoped<IPronunciationAudioSynthesizer, PronunciationAudioSynthesizer>();
        services.AddScoped<IScoringRubricProvider, ScoringRubricProvider>();
        services.AddScoped<IPronunciationTargetResolver, PronunciationTargetResolver>();
        services.AddScoped<ILessonRubricDetailBuilder, LessonRubricDetailBuilder>();

        return services;
    }
}
