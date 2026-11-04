using Microsoft.Extensions.DependencyInjection;

namespace EnglishLiveTutor.Modules.Progress;

public static class ProgressModuleServiceCollectionExtensions
{
    public static IServiceCollection AddProgressModule(this IServiceCollection services)
    {
        return services;
    }
}
