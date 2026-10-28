using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Infrastructure.Identity;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class DatabaseSeederAdminUserTests
{
    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public FakeHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "EnglishLiveTutor.Domain.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private static AppDbContext CreateInMemoryContext(string dbName)
    {
        var interceptor = new AuditingSaveChangesInterceptor(TimeProvider.System, null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(interceptor)
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void GetAdminUserOptions_WhenConfigurationIsNull_ShouldReturnDefaultsWithNullPassword()
    {
        var options = DatabaseSeeder.GetAdminUserOptions(null);

        Assert.NotNull(options);
        Assert.Equal("admin", options.Username);
        Assert.Equal("admin@lingomint.local", options.Email);
        Assert.Null(options.Password);
        Assert.Equal("LingoMint Administrator", options.FullName);
        Assert.Equal("C1", options.PreferredCefrLevel);
        Assert.Equal("Aoede", options.PreferredVoiceName);
    }

    [Fact]
    public void GetAdminUserOptions_WhenAdminUserSectionConfigured_ShouldBindCorrectly()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["AdminUser:Username"] = "superadmin",
            ["AdminUser:Email"] = "superadmin@domain.com",
            ["AdminUser:Password"] = "SuperSecret#2026",
            ["AdminUser:FullName"] = "Super Administrator",
            ["AdminUser:PreferredCefrLevel"] = "C2",
            ["AdminUser:PreferredVoiceName"] = "Puck"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var options = DatabaseSeeder.GetAdminUserOptions(config);

        Assert.NotNull(options);
        Assert.Equal("superadmin", options.Username);
        Assert.Equal("superadmin@domain.com", options.Email);
        Assert.Equal("SuperSecret#2026", options.Password);
        Assert.Equal("Super Administrator", options.FullName);
        Assert.Equal("C2", options.PreferredCefrLevel);
        Assert.Equal("Puck", options.PreferredVoiceName);
    }

    [Fact]
    public void GetAdminUserOptions_WhenSeedDataAdminUserConfigured_ShouldNotBindAndPasswordRemainsNull()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["SeedData:AdminUser:Username"] = "seedadmin",
            ["SeedData:AdminUser:Password"] = "SeedPass#2026"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var options = DatabaseSeeder.GetAdminUserOptions(config);

        Assert.NotNull(options);
        Assert.Equal("admin", options.Username);
        Assert.Null(options.Password);
    }

    [Fact]
    public void GetDemoLearnerOptions_WhenDemoLearnerSectionConfigured_ShouldBindCorrectly()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["DemoLearner:Username"] = "custom_learner",
            ["DemoLearner:Password"] = "CustomPass#2026"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var options = DatabaseSeeder.GetDemoLearnerOptions(config);

        Assert.NotNull(options);
        Assert.Equal("custom_learner", options.Username);
        Assert.Equal("CustomPass#2026", options.Password);
    }

    [Fact]
    public void GetDemoLearnerOptions_WhenSeedDataDemoLearnerConfigured_ShouldNotBindAndPasswordRemainsNull()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["SeedData:DemoLearner:Username"] = "seedlearner",
            ["SeedData:DemoLearner:Password"] = "SeedPass#2026"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var options = DatabaseSeeder.GetDemoLearnerOptions(config);

        Assert.NotNull(options);
        Assert.Equal("default-learner", options.Username);
        Assert.Null(options.Password);
    }

    [Fact]
    public async Task SeedIdentityAsync_WhenAdminPasswordMissing_ShouldNotSeedAdminUser()
    {
        string dbName = Guid.NewGuid().ToString("N");
        await using var db = CreateInMemoryContext(dbName);

        var adminOptions = new AdminUserSeedOptions
        {
            Username = "admin",
            Password = null
        };

        await DatabaseSeeder.SeedIdentityAsync(db, adminOptions: adminOptions);

        var admin = await db.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        Assert.Null(admin);
    }

    [Fact]
    public async Task SeedIdentityAsync_WhenAdminPasswordTooShort_ShouldNotSeedAdminUser()
    {
        string dbName = Guid.NewGuid().ToString("N");
        await using var db = CreateInMemoryContext(dbName);

        var adminOptions = new AdminUserSeedOptions
        {
            Username = "admin",
            Password = "short" // < 8 characters
        };

        await DatabaseSeeder.SeedIdentityAsync(db, adminOptions: adminOptions);

        var admin = await db.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        Assert.Null(admin);
    }

    [Fact]
    public async Task SeedIdentityAsync_WhenAdminPasswordValid_ShouldSeedAdminUserAndAssignAdminRole()
    {
        string dbName = Guid.NewGuid().ToString("N");
        await using var db = CreateInMemoryContext(dbName);

        var adminOptions = new AdminUserSeedOptions
        {
            Username = "custom_admin",
            Email = "custom_admin@lingomint.local",
            Password = "SecureAdminPass#2026",
            FullName = "Custom Admin User",
            PreferredCefrLevel = "C1",
            PreferredVoiceName = "Kore"
        };

        await DatabaseSeeder.SeedIdentityAsync(db, adminOptions: adminOptions);

        var admin = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Username == "custom_admin");

        Assert.NotNull(admin);
        Assert.Equal("custom_admin@lingomint.local", admin.Email);
        Assert.Equal("Custom Admin User", admin.FullName);

        var hasher = new Pbkdf2PasswordHasher();
        Assert.True(hasher.VerifyPassword("SecureAdminPass#2026", admin.PasswordHash));

        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Code == "ADMIN");
        Assert.NotNull(adminRole);
        Assert.Contains(admin.UserRoles, ur => ur.RoleId == adminRole.Id);
    }

    [Fact]
    public async Task SeedIdentityAsync_NonDevelopment_ShouldNotSeedDefaultLearner()
    {
        string dbName = Guid.NewGuid().ToString("N");
        await using var db = CreateInMemoryContext(dbName);

        var env = new FakeHostEnvironment(Environments.Production);
        var demoOptions = new DemoLearnerSeedOptions
        {
            Password = "LearnerPass#2026"
        };

        await DatabaseSeeder.SeedIdentityAsync(db, demoLearnerOptions: demoOptions, environment: env);

        var learner = await db.Users.FirstOrDefaultAsync(u => u.Username == "default-learner");
        Assert.Null(learner);
    }

    [Fact]
    public async Task SeedIdentityAsync_DevelopmentWithoutDemoPassword_ShouldNotSeedDefaultLearner()
    {
        string dbName = Guid.NewGuid().ToString("N");
        await using var db = CreateInMemoryContext(dbName);

        var env = new FakeHostEnvironment(Environments.Development);
        var demoOptions = new DemoLearnerSeedOptions
        {
            Password = null
        };

        await DatabaseSeeder.SeedIdentityAsync(db, demoLearnerOptions: demoOptions, environment: env);

        var learner = await db.Users.FirstOrDefaultAsync(u => u.Username == "default-learner");
        Assert.Null(learner);
    }

    [Fact]
    public async Task SeedIdentityAsync_DevelopmentWithDemoPassword_ShouldSeedDefaultLearner()
    {
        string dbName = Guid.NewGuid().ToString("N");
        await using var db = CreateInMemoryContext(dbName);

        var env = new FakeHostEnvironment(Environments.Development);
        var demoOptions = new DemoLearnerSeedOptions
        {
            Username = "default-learner",
            Password = "LearnerPass#2026"
        };

        await DatabaseSeeder.SeedIdentityAsync(db, demoLearnerOptions: demoOptions, environment: env);

        var learner = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Username == "default-learner");

        Assert.NotNull(learner);

        var hasher = new Pbkdf2PasswordHasher();
        Assert.True(hasher.VerifyPassword("LearnerPass#2026", learner.PasswordHash));

        var learnerRole = await db.Roles.FirstOrDefaultAsync(r => r.Code == "LEARNER");
        Assert.NotNull(learnerRole);
        Assert.Contains(learner.UserRoles, ur => ur.RoleId == learnerRole.Id);
    }

    [Fact]
    public async Task SeedIdentityAsync_IsIdempotent()
    {
        string dbName = Guid.NewGuid().ToString("N");
        await using var db = CreateInMemoryContext(dbName);

        var env = new FakeHostEnvironment(Environments.Development);
        var adminOptions = new AdminUserSeedOptions
        {
            Username = "admin",
            Password = "AdminPass#2026"
        };
        var demoOptions = new DemoLearnerSeedOptions
        {
            Username = "default-learner",
            Password = "LearnerPass#2026"
        };

        await DatabaseSeeder.SeedIdentityAsync(db, adminOptions, demoOptions, env);
        await DatabaseSeeder.SeedIdentityAsync(db, adminOptions, demoOptions, env);

        var adminCount = await db.Users.CountAsync(u => u.Username == "admin");
        var learnerCount = await db.Users.CountAsync(u => u.Username == "default-learner");

        Assert.Equal(1, adminCount);
        Assert.Equal(1, learnerCount);
    }
}
