using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Modules;
using EnglishLiveTutor.Application.Modules.Catalog;
using EnglishLiveTutor.Application.Modules.Grammar;
using EnglishLiveTutor.Application.Modules.Identity;
using EnglishLiveTutor.Application.Modules.Lessons;
using EnglishLiveTutor.Application.Modules.LiveTutor;
using EnglishLiveTutor.Application.Modules.Speech;
using EnglishLiveTutor.Application.Modules.Vocabulary;
using EnglishLiveTutor.Infrastructure;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Modules;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishLiveTutor.Domain.Tests.Architecture;

public sealed class ModuleDbContextRegistrationTests
{
    private static readonly Type[] ModuleDbContextTypes =
    [
        typeof(IIdentityDbContext),
        typeof(ICatalogDbContext),
        typeof(ISpeechDbContext),
        typeof(ILessonsDbContext),
        typeof(IGrammarDbContext),
        typeof(IVocabularyDbContext),
        typeof(ILiveTutorDbContext)
    ];

    [Fact]
    public void ModuleDbContextInterfaces_ResolveToDistinctTypedInstancesSharingOneConnection()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var sharedDbContext = scope.ServiceProvider.GetRequiredService<SharedDbContext>();
        var sharedConnection = scope.ServiceProvider.GetRequiredService<SqlConnection>();
        var moduleContexts = new List<object>();

        foreach (var moduleDbContextType in ModuleDbContextTypes)
        {
            var moduleDbContext = scope.ServiceProvider.GetRequiredService(moduleDbContextType);
            moduleContexts.Add(moduleDbContext);
            Assert.IsAssignableFrom<DbContext>(moduleDbContext);
        }

        Assert.Equal(ModuleDbContextTypes.Length, moduleContexts.Distinct().Count());
        Assert.IsType<SharedDbContext>(sharedDbContext);
        Assert.Same(sharedConnection, sharedDbContext.Database.GetDbConnection());
        Assert.All(
            moduleContexts.Cast<DbContext>(),
            context => Assert.Same(sharedConnection, context.Database.GetDbConnection()));
        Assert.IsType<IdentityDbContext>(
            scope.ServiceProvider.GetRequiredService<IIdentityDbContext>());
        Assert.IsType<CatalogDbContext>(
            scope.ServiceProvider.GetRequiredService<ICatalogDbContext>());
        Assert.IsType<SpeechDbContext>(
            scope.ServiceProvider.GetRequiredService<ISpeechDbContext>());
        Assert.IsType<LessonsDbContext>(
            scope.ServiceProvider.GetRequiredService<ILessonsDbContext>());
        Assert.IsType<GrammarDbContext>(
            scope.ServiceProvider.GetRequiredService<IGrammarDbContext>());
        Assert.IsType<VocabularyDbContext>(
            scope.ServiceProvider.GetRequiredService<IVocabularyDbContext>());
        Assert.IsType<LiveTutorDbContext>(
            scope.ServiceProvider.GetRequiredService<ILiveTutorDbContext>());
    }

    [Fact]
    public void VocabularyModuleDbContextFactory_IsResolvable()
    {
        using var provider = CreateProvider();

        var factory = provider.GetRequiredService<IModuleDbContextFactory<IVocabularyDbContext>>();

        Assert.IsType<ModuleDbContextFactory<IVocabularyDbContext, VocabularyDbContext>>(factory);
    }

    [Fact]
    public void ApplicationModuleNamespaces_UsesModuleBoundaryRules()
    {
        var path = "Modules/Grammar/EnglishLiveTutor.Modules.Grammar.Application/Features/Handler.cs";
        var violations = ModuleBoundaryScanner.ScanFile(
            path,
            "using EnglishLiveTutor.Application.Modules.Lessons;");

        Assert.Contains(
            new Violation(
                "internal",
                path,
                "EnglishLiveTutor.Application.Modules.Lessons"),
            violations);
        Assert.Equal(
            ModuleName.Shared,
            ModuleMap.ModuleOfNamespace("EnglishLiveTutor.Application.Modules"));
        Assert.Equal(
            ModuleName.Vocabulary,
            ModuleMap.ModuleOfFile(
                "Modules/Vocabulary/EnglishLiveTutor.Modules.Vocabulary.Application/Modules/Vocabulary/IVocabularyDbContext.cs"));
        Assert.Equal(
            ModuleName.Shared,
            ModuleMap.ModuleOfFile(
                "Shared/EnglishLiveTutor.Shared.Application/Persistence/IModuleDbContext.cs"));
    }

    [Fact]
    public void ModuleDbContextInterfaces_ExposeAllModuleDbSetsWithoutDuplicates()
    {
        var propertyNames = ModuleDbContextTypes
            .SelectMany(type => type.GetProperties())
            .Where(IsDbSetProperty)
            .Select(property => property.Name)
            .ToArray();
        var appDbContextPropertyNames = typeof(IAppDbContext)
            .GetInterfaces()
            .SelectMany(type => type.GetProperties())
            .Where(IsDbSetProperty)
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(48, propertyNames.Length);
        Assert.Equal(48, propertyNames.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(
            appDbContextPropertyNames.Order(StringComparer.Ordinal),
            propertyNames.Order(StringComparer.Ordinal));
        Assert.DoesNotContain("IdempotencyRecords", propertyNames);
        Assert.Contains(
            typeof(AppDbContext).GetProperties(),
            property => property.Name == "IdempotencyRecords");
    }

    private static ServiceProvider CreateProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Server=(localdb)\\mssqllocaldb;Database=ModuleDbContextTests;Trusted_Connection=True;"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddInfrastructureServices(configuration);
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
    }

    private static bool IsDbSetProperty(System.Reflection.PropertyInfo property)
    {
        return property.PropertyType.IsGenericType &&
               property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>);
    }
}
