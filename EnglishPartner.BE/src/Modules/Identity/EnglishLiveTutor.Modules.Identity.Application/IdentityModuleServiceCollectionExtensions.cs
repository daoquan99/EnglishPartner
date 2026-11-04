using EnglishLiveTutor.Application.Features.Identity.Contracts;
using EnglishLiveTutor.Application.Features.Identity.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishLiveTutor.Modules.Identity;

public static class IdentityModuleServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        services.AddScoped<IUserDirectory, UserDirectory>();

        return services;
    }
}
