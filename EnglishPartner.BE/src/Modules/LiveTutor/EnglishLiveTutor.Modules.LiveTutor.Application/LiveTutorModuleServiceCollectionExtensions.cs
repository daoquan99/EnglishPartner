using Microsoft.Extensions.DependencyInjection;

namespace EnglishLiveTutor.Modules.LiveTutor;

public static class LiveTutorModuleServiceCollectionExtensions
{
    public static IServiceCollection AddLiveTutorModule(this IServiceCollection services)
    {
        return services;
    }
}
