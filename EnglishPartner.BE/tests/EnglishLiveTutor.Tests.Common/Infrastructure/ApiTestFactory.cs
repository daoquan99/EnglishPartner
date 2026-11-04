using System.Net.Http.Headers;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Modules.Catalog;
using EnglishLiveTutor.Application.Modules.Grammar;
using EnglishLiveTutor.Application.Modules.Identity;
using EnglishLiveTutor.Application.Modules.Lessons;
using EnglishLiveTutor.Application.Modules.LiveTutor;
using EnglishLiveTutor.Application.Modules.Speech;
using EnglishLiveTutor.Application.Modules.Vocabulary;
using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Interceptors;
using EnglishLiveTutor.Infrastructure.Persistence.Modules;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishLiveTutor.Domain.Tests.Infrastructure;

public class ApiTestFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "TestDb_" + Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "test-jwt-secret-key-that-is-at-least-32-bytes-long!",
                ["Jwt:Issuer"] = "EnglishLiveTutor.Api",
                ["Jwt:Audience"] = "EnglishLiveTutor.Client",
                ["Database:MigrateOnStartup"] = "false",
                ["Database:SeedOnStartup"] = "false",
                ["ConnectionStrings:DefaultConnection"] = "Server=none;Database=none;Trusted_Connection=True;",
                ["AllowedHosts"] = "*",
                ["RateLimiting:Auth:PermitLimit"] = "100000",
                ["RateLimiting:Auth:WindowSeconds"] = "60",
                ["RateLimiting:AuthRefresh:PermitLimit"] = "100000",
                ["RateLimiting:AuthRefresh:WindowSeconds"] = "60",
                ["RateLimiting:Ai:TokenLimit"] = "100000",
                ["RateLimiting:Ai:TokensPerPeriod"] = "100000",
                ["RateLimiting:Ai:ReplenishmentPeriodSeconds"] = "60"
            });
        });

        builder.ConfigureServices(services =>
        {
            Type[] contextTypes =
            [
                typeof(AppDbContext),
                typeof(SharedDbContext),
                typeof(IdentityDbContext),
                typeof(CatalogDbContext),
                typeof(SpeechDbContext),
                typeof(LessonsDbContext),
                typeof(GrammarDbContext),
                typeof(VocabularyDbContext),
                typeof(LiveTutorDbContext)
            ];
            Type[] moduleInterfaceTypes =
            [
                typeof(IAppDbContext),
                typeof(IIdentityDbContext),
                typeof(ICatalogDbContext),
                typeof(ISpeechDbContext),
                typeof(ILessonsDbContext),
                typeof(IGrammarDbContext),
                typeof(IVocabularyDbContext),
                typeof(ILiveTutorDbContext)
            ];

            var descriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions) ||
                contextTypes.Contains(d.ServiceType) ||
                moduleInterfaceTypes.Contains(d.ServiceType) ||
                (d.ServiceType.IsGenericType &&
                 (d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>) ||
                  d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextFactory<>) ||
                  d.ServiceType.GetGenericTypeDefinition().Name == "IDbContextOptionsConfiguration`1") &&
                 contextTypes.Contains(d.ServiceType.GetGenericArguments()[0]))).ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            var inMemoryServiceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContextFactory<AppDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(_databaseName)
                       .UseInternalServiceProvider(inMemoryServiceProvider);
                options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                options.AddInterceptors(sp.GetRequiredService<AuditingSaveChangesInterceptor>());
            });
            services.AddDbContextFactory<SharedDbContext>(ConfigureInMemory);
            services.AddDbContextFactory<IdentityDbContext>(ConfigureInMemory);
            services.AddDbContextFactory<CatalogDbContext>(ConfigureInMemory);
            services.AddDbContextFactory<SpeechDbContext>(ConfigureInMemory);
            services.AddDbContextFactory<LessonsDbContext>(ConfigureInMemory);
            services.AddDbContextFactory<GrammarDbContext>(ConfigureInMemory);
            services.AddDbContextFactory<VocabularyDbContext>(ConfigureInMemory);
            services.AddDbContextFactory<LiveTutorDbContext>(ConfigureInMemory);

            services.AddScoped<AppDbContext>(sp => sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());
            services.AddScoped<SharedDbContext>(sp => sp.GetRequiredService<IDbContextFactory<SharedDbContext>>().CreateDbContext());
            services.AddScoped<IdentityDbContext>(sp => sp.GetRequiredService<IDbContextFactory<IdentityDbContext>>().CreateDbContext());
            services.AddScoped<CatalogDbContext>(sp => sp.GetRequiredService<IDbContextFactory<CatalogDbContext>>().CreateDbContext());
            services.AddScoped<SpeechDbContext>(sp => sp.GetRequiredService<IDbContextFactory<SpeechDbContext>>().CreateDbContext());
            services.AddScoped<LessonsDbContext>(sp => sp.GetRequiredService<IDbContextFactory<LessonsDbContext>>().CreateDbContext());
            services.AddScoped<GrammarDbContext>(sp => sp.GetRequiredService<IDbContextFactory<GrammarDbContext>>().CreateDbContext());
            services.AddScoped<VocabularyDbContext>(sp => sp.GetRequiredService<IDbContextFactory<VocabularyDbContext>>().CreateDbContext());
            services.AddScoped<LiveTutorDbContext>(sp => sp.GetRequiredService<IDbContextFactory<LiveTutorDbContext>>().CreateDbContext());
            services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
            services.AddScoped<IIdentityDbContext>(sp => sp.GetRequiredService<IdentityDbContext>());
            services.AddScoped<ICatalogDbContext>(sp => sp.GetRequiredService<CatalogDbContext>());
            services.AddScoped<ISpeechDbContext>(sp => sp.GetRequiredService<SpeechDbContext>());
            services.AddScoped<ILessonsDbContext>(sp => sp.GetRequiredService<LessonsDbContext>());
            services.AddScoped<IGrammarDbContext>(sp => sp.GetRequiredService<GrammarDbContext>());
            services.AddScoped<IVocabularyDbContext>(sp => sp.GetRequiredService<VocabularyDbContext>());
            services.AddScoped<ILiveTutorDbContext>(sp => sp.GetRequiredService<LiveTutorDbContext>());

            void ConfigureInMemory(IServiceProvider sp, DbContextOptionsBuilder options)
            {
                options.UseInMemoryDatabase(_databaseName)
                       .UseInternalServiceProvider(inMemoryServiceProvider);
                options.ConfigureWarnings(w =>
                    w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                options.AddInterceptors(sp.GetRequiredService<AuditingSaveChangesInterceptor>());
            }
        });
    }

    public string CreateAccessToken(
        Guid userId,
        string username = "testuser",
        string email = "test@example.com",
        string role = "ADMIN",
        IEnumerable<string>? permissions = null)
    {
        var tokenService = Services.GetRequiredService<IJwtTokenService>();
        var user = new UserEntity(
            username: username,
            email: email,
            fullName: username,
            passwordHash: "dummyHash",
            id: userId);

        var roles = new[] { role };
        var perms = permissions?.ToList() ?? new List<string>
        {
            "lesson-exercises.attempt",
            "daily-learning.view",
            "daily-learning.review",
            "pronunciation.assess"
        };

        var generated = tokenService.GenerateAccessToken(user, roles, perms);
        return generated.AccessToken;
    }

    public HttpClient CreateAuthenticatedClient(
        Guid userId,
        string username = "testuser",
        string email = "test@example.com",
        string role = "ADMIN",
        IEnumerable<string>? permissions = null)
    {
        var token = CreateAccessToken(userId, username, email, role, permissions);
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public HttpClient CreateAnonymousClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }

    public async Task ExecuteDbContextAsync(Func<AppDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var db = await factory.CreateDbContextAsync();
        await action(db);
        await db.SaveChangesAsync();
    }
}
