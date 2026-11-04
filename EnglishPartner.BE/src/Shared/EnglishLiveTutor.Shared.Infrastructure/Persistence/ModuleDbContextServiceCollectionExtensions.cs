using System.Linq.Expressions;
using System.Reflection;
using EnglishLiveTutor.Application.Modules;
using EnglishLiveTutor.Infrastructure.Persistence.Interceptors;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnglishLiveTutor.Infrastructure.Persistence;

public static class ModuleDbContextServiceCollectionExtensions
{
    public static IServiceCollection AddModuleDbContext<TContext, TInterface>(
        this IServiceCollection services,
        IConfiguration configuration,
        string schema)
        where TContext : DbContext, TInterface
        where TInterface : class, IModuleDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(schema);

        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException(
                "Connection string is not configured. Configure ConnectionStrings:DefaultConnection or ConnectionStrings:SqlServer.");

        services.AddDbContextFactory<TContext>((sp, options) =>
        {
            options.UseSqlServer(
                connectionString,
                sqlOptions => ConfigureSqlServerOptions(sqlOptions, typeof(TContext).Assembly, schema));
            ConfigureCommonOptions(options, sp);
        });

        services.AddScoped(sp => CreateScopedContext<TContext>(sp, schema));
        services.AddScoped<TInterface>(sp => sp.GetRequiredService<TContext>());
        services.AddSingleton<IModuleDbContextFactory<TInterface>>(sp =>
            new ModuleDbContextFactory<TInterface, TContext>(
                sp.GetRequiredService<IDbContextFactory<TContext>>()));
        services.AddScoped<IUnitOfWorkParticipant>(sp =>
            new UnitOfWorkParticipant<TContext>(sp.GetRequiredService<TContext>()));

        return services;
    }

    public static IServiceCollection AddSharedDbContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException(
                "Connection string is not configured. Configure ConnectionStrings:DefaultConnection or ConnectionStrings:SqlServer.");

        services.AddDbContextFactory<SharedDbContext>((sp, options) =>
        {
            options.UseSqlServer(
                connectionString,
                sqlOptions => ConfigureSqlServerOptions(sqlOptions, typeof(SharedDbContext).Assembly, ModuleSchemas.Shared));
            ConfigureCommonOptions(options, sp);
        });

        services.AddScoped(sp => CreateScopedContext<SharedDbContext>(sp, ModuleSchemas.Shared));
        services.AddScoped<IUnitOfWorkParticipant>(sp =>
            new UnitOfWorkParticipant<SharedDbContext>(sp.GetRequiredService<SharedDbContext>()));

        return services;
    }

    public static void ConfigureSqlServerOptions(
        SqlServerDbContextOptionsBuilder sqlOptions,
        Assembly migrationsAssembly,
        string schema)
    {
        ArgumentNullException.ThrowIfNull(sqlOptions);
        ArgumentNullException.ThrowIfNull(migrationsAssembly);
        ArgumentNullException.ThrowIfNull(schema);

        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        sqlOptions.MigrationsAssembly(migrationsAssembly.FullName);
        sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", schema);
    }

    public static void ConfigureCommonOptions(
        DbContextOptionsBuilder options,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        options.AddInterceptors(serviceProvider.GetRequiredService<AuditingSaveChangesInterceptor>());
    }

    public static TContext CreateScopedContext<TContext>(
        IServiceProvider serviceProvider,
        string schema,
        Func<DbContextOptions<TContext>, TContext>? factory = null,
        bool ignorePendingModelChanges = false)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(schema);

        var options = new DbContextOptionsBuilder<TContext>();
        options.UseLoggerFactory(serviceProvider.GetRequiredService<ILoggerFactory>());
        options.UseApplicationServiceProvider(serviceProvider);
        options.UseSqlServer(
            serviceProvider.GetRequiredService<SqlConnection>(),
            sqlOptions => ConfigureSqlServerOptions(sqlOptions, typeof(TContext).Assembly, schema));
        ConfigureCommonOptions(options, serviceProvider);
        if (ignorePendingModelChanges)
        {
            options.ConfigureWarnings(w =>
                w.Ignore(RelationalEventId.PendingModelChangesWarning));
        }

        return factory is not null
            ? factory(options.Options)
            : ContextActivator<TContext>.Create(options.Options);
    }

    private static class ContextActivator<TContext> where TContext : DbContext
    {
        public static readonly Func<DbContextOptions<TContext>, TContext> Create = CreateFactory();

        private static Func<DbContextOptions<TContext>, TContext> CreateFactory()
        {
            var specificCtor = typeof(TContext).GetConstructor([typeof(DbContextOptions<TContext>)]);
            if (specificCtor is not null)
            {
                var param = Expression.Parameter(typeof(DbContextOptions<TContext>), "options");
                return Expression.Lambda<Func<DbContextOptions<TContext>, TContext>>(
                    Expression.New(specificCtor, param),
                    param).Compile();
            }

            var generalCtor = typeof(TContext).GetConstructor([typeof(DbContextOptions)]);
            if (generalCtor is not null)
            {
                var param = Expression.Parameter(typeof(DbContextOptions<TContext>), "options");
                return Expression.Lambda<Func<DbContextOptions<TContext>, TContext>>(
                    Expression.New(generalCtor, param),
                    param).Compile();
            }

            return options => (TContext)Activator.CreateInstance(typeof(TContext), options)!;
        }
    }
}
