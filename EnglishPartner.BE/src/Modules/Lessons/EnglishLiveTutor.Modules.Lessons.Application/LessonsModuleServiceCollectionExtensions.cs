using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Lessons.Services;
using EnglishLiveTutor.Application.Features.Lessons.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishLiveTutor.Modules.Lessons;

public static class LessonsModuleServiceCollectionExtensions
{
    public static IServiceCollection AddLessonsModule(this IServiceCollection services)
    {
        services.AddScoped<ILessonCandidateValidator, LessonCandidateValidator>();
        services.AddScoped<ILessonAttemptResumeBuilder, LessonAttemptResumeBuilder>();
        services.AddScoped<ILessonDetailBuilder, LessonDetailBuilder>();

        return services;
    }
}
