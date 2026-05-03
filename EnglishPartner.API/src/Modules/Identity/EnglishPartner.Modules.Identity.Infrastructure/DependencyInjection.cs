using EnglishPartner.Modules.Identity.Application.Abstractions;
using EnglishPartner.Modules.Identity.Domain.Roles.Repositories;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.Modules.Identity.Infrastructure.Persistence;
using EnglishPartner.Modules.Identity.Infrastructure.Persistence.Repositories;
using EnglishPartner.Modules.Identity.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishPartner.Modules.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EnglishPartner.Modules.Identity.Application.Auth.Settings.SocialAuthSettings>(
            configuration.GetSection(EnglishPartner.Modules.Identity.Application.Auth.Settings.SocialAuthSettings.SectionName));

        services.AddDbContext<IdentityDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("Database"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", IdentityDbContext.Schema));
        });

        // IIdentityUnitOfWork → IdentityDbContext (for module-scoped UoW behavior in Application)
        services.AddScoped<IIdentityUnitOfWork>(sp => sp.GetRequiredService<IdentityDbContext>());

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IStreakRepository, StreakRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IOnboardingAnswerRepository, OnboardingAnswerRepository>();

        // Services
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<ISocialAuthService, SocialAuthService>();
        services.AddScoped<ISessionService, SessionService>();  // Redis session store for JWT invalidation


        // Startup seeder — called from Program.cs after migrations
        services.AddScoped<AdminUserSeeder>();

        return services;
    }
}
