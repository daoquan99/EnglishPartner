using EnglishPartner.Modules.Learning.Application.Abstractions;
using EnglishPartner.Modules.Learning.Contracts.Services;
using EnglishPartner.Modules.Learning.Domain.ItemMasteries.Repositories;
using EnglishPartner.Modules.Learning.Domain.StudySessions.Repositories;
using EnglishPartner.Modules.Learning.Domain.UserProfiles.Repositories;
using EnglishPartner.Modules.Learning.Infrastructure.Persistence;
using EnglishPartner.Modules.Learning.Infrastructure.Persistence.Repositories;
using EnglishPartner.Modules.Learning.Infrastructure.Services;
using EnglishPartner.SharedKernel.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishPartner.Modules.Learning.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLearningInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LearningDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Database"),
                       npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", LearningDbContext.Schema));
        });

        services.AddScoped<ILearningUnitOfWork>(sp => sp.GetRequiredService<LearningDbContext>());

        services.AddScoped<IUserLearningProfileRepository, UserLearningProfileRepository>();
        services.AddScoped<IItemMasteryRepository, ItemMasteryRepository>();
        services.AddScoped<IStudySessionRepository, StudySessionRepository>();
        services.AddScoped<ILearningActivityLogRepository, LearningActivityLogRepository>();

        // Module Interface — consumed by other modules (e.g. AI)
        services.AddScoped<ILearningProfileQueryService, LearningProfileQueryService>();

        return services;
    }
}
