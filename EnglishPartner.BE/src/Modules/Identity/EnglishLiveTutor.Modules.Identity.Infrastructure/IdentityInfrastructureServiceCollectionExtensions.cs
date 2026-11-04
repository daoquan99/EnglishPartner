using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Modules.Identity;
using EnglishLiveTutor.Domain.Identity.Repositories;
using EnglishLiveTutor.Infrastructure.Identity;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Modules;
using EnglishLiveTutor.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishLiveTutor.Modules.Identity.Infrastructure;

public static class IdentityInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddModuleDbContext<IdentityDbContext, IIdentityDbContext>(
            configuration,
            ModuleSchemas.Identity);

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

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();

        return services;
    }
}
