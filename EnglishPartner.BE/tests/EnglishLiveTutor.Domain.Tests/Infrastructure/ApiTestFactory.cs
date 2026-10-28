using System.Net.Http.Headers;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Interceptors;
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
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(IDbContextFactory<AppDbContext>) ||
                d.ServiceType == typeof(AppDbContext) ||
                d.ServiceType == typeof(IAppDbContext)).ToList();

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

            services.AddScoped<AppDbContext>(sp => sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());
            services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
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
