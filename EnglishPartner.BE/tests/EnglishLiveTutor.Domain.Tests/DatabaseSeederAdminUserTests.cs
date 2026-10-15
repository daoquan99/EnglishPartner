using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Infrastructure.Identity;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class DatabaseSeederAdminUserTests
{
    private static AppDbContext CreateInMemoryContext(string dbName)
    {
        var interceptor = new AuditingSaveChangesInterceptor(TimeProvider.System, SystemCurrentUser.Instance);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(interceptor)
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void GetAdminUserOptions_WhenConfigurationIsNull_ShouldReturnDefaults()
    {
        var options = DatabaseSeeder.GetAdminUserOptions(null);

        Assert.NotNull(options);
        Assert.Equal("admin", options.Username);
        Assert.Equal("admin@lingomint.local", options.Email);
        Assert.Equal("Admin@123", options.Password);
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
    public void GetAdminUserOptions_WhenSeedDataAdminUserSectionConfigured_ShouldBindCorrectly()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["SeedData:AdminUser:Username"] = "rootadmin",
            ["SeedData:AdminUser:Email"] = "root@lingomint.local",
            ["SeedData:AdminUser:Password"] = "RootPass#123",
            ["SeedData:AdminUser:FullName"] = "Root Administrator"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var options = DatabaseSeeder.GetAdminUserOptions(config);

        Assert.NotNull(options);
        Assert.Equal("rootadmin", options.Username);
        Assert.Equal("root@lingomint.local", options.Email);
        Assert.Equal("RootPass#123", options.Password);
        Assert.Equal("Root Administrator", options.FullName);
    }

    [Fact]
    public async Task SeedIdentityAsync_ShouldSeedAdminUserFromConfiguration_AndAssignAdminRole()
    {
        string dbName = Guid.NewGuid().ToString("N");
        await using var db = CreateInMemoryContext(dbName);

        var inMemorySettings = new Dictionary<string, string?>
        {
            ["AdminUser:Username"] = "custom_admin",
            ["AdminUser:Email"] = "custom_admin@lingomint.local",
            ["AdminUser:Password"] = "SecureAdminPass#2026",
            ["AdminUser:FullName"] = "Custom Admin User",
            ["AdminUser:PreferredCefrLevel"] = "C1",
            ["AdminUser:PreferredVoiceName"] = "Kore"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act: Seed identity with custom configuration
        await DatabaseSeeder.SeedIdentityAsync(db, config);

        // Assert: Admin user exists with correct configured data
        var admin = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Username == "custom_admin");

        Assert.NotNull(admin);
        Assert.Equal("custom_admin@lingomint.local", admin.Email);
        Assert.Equal("CUSTOM_ADMIN@LINGOMINT.LOCAL", admin.NormalizedEmail);
        Assert.Equal("Custom Admin User", admin.FullName);
        Assert.Equal("C1", admin.PreferredCefrLevel);
        Assert.Equal("Kore", admin.PreferredVoiceName);

        // Assert: Password verification succeeds
        var hasher = new Pbkdf2PasswordHasher();
        Assert.True(hasher.VerifyPassword("SecureAdminPass#2026", admin.PasswordHash));
        Assert.False(hasher.VerifyPassword("WrongPassword", admin.PasswordHash));

        // Assert: ADMIN role is assigned
        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Code == "ADMIN");
        Assert.NotNull(adminRole);
        Assert.Contains(admin.UserRoles, ur => ur.RoleId == adminRole.Id);

        // Assert: Default learner is also seeded
        var learner = await db.Users.FirstOrDefaultAsync(u => u.Username == "default-learner");
        Assert.NotNull(learner);

        // Act 2: Idempotency check - calling again should succeed without duplicates
        await DatabaseSeeder.SeedIdentityAsync(db, config);
        var adminCount = await db.Users.CountAsync(u => u.Username == "custom_admin");
        Assert.Equal(1, adminCount);
    }
}
