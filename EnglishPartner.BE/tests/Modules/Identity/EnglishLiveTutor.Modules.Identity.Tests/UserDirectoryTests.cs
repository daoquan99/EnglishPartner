using EnglishLiveTutor.Application.Features.Identity.Services;
using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public sealed class UserDirectoryTests
{
    [Fact]
    public async Task GetLearningProfileAsync_ByIdString_ReturnsProfile()
    {
        await using var db = CreateContext();
        var user = CreateUser("alice", "Alice Example", "Europe/London", "C1");
        db.Users.AddRange(
            user,
            CreateUser("user-b", "User B", "Asia/Tokyo", "A2"));
        await db.SaveChangesAsync();

        var result = await new UserDirectory(db).GetLearningProfileAsync(user.Id.ToString());

        Assert.NotNull(result);
        Assert.Equal("Europe/London", result.TimeZoneId);
        Assert.Equal("C1", result.PreferredCefrLevel);
    }

    [Fact]
    public async Task GetLearningProfileAsync_ByUsername_ReturnsProfile()
    {
        await using var db = CreateContext();
        db.Users.AddRange(
            CreateUser("alice", "Alice Example", "Europe/London", "C1"),
            CreateUser("bob", "Bob Example", "Asia/Tokyo", "A2"));
        await db.SaveChangesAsync();

        var result = await new UserDirectory(db).GetLearningProfileAsync("bob");

        Assert.NotNull(result);
        Assert.Equal("Asia/Tokyo", result.TimeZoneId);
        Assert.Equal("A2", result.PreferredCefrLevel);
    }

    [Fact]
    public async Task GetLearningProfileAsync_UnknownUser_ReturnsNull()
    {
        await using var db = CreateContext();

        var result = await new UserDirectory(db).GetLearningProfileAsync("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task ListUsersByIdOrUsernameAsync_MatchesEitherIdentifier()
    {
        await using var db = CreateContext();
        var alice = CreateUser("alice", "Alice", "UTC", "B1");
        var bob = CreateUser("bob", "Bob", "UTC", "B2");
        db.Users.AddRange(alice, bob);
        await db.SaveChangesAsync();

        var result = await new UserDirectory(db)
            .ListUsersByIdOrUsernameAsync([alice.Id.ToString(), "bob"]);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, user => user.Id == alice.Id);
        Assert.Contains(result, user => user.Username == "bob");
    }

    [Fact]
    public async Task ListUsersByIdsAsync_ReturnsMatchingUsers()
    {
        await using var db = CreateContext();
        var alice = CreateUser("alice", "Alice", "UTC", "B1");
        var bob = CreateUser("bob", "Bob", "UTC", "B2");
        db.Users.AddRange(alice, bob);
        await db.SaveChangesAsync();

        var result = await new UserDirectory(db).ListUsersByIdsAsync([alice.Id]);

        var user = Assert.Single(result);
        Assert.Equal(alice.Id, user.Id);
        Assert.Equal("alice", user.Username);
    }

    [Fact]
    public async Task ListUsersByUsernamesAsync_ReturnsMatchingUsers()
    {
        await using var db = CreateContext();
        db.Users.AddRange(
            CreateUser("alice", "Alice", "UTC", "B1"),
            CreateUser("bob", "Bob", "UTC", "B2"));
        await db.SaveChangesAsync();

        var result = await new UserDirectory(db).ListUsersByUsernamesAsync(["bob"]);

        var user = Assert.Single(result);
        Assert.Equal("bob", user.Username);
        Assert.Equal("Bob", user.FullName);
    }

    private static UserEntity CreateUser(
        string username,
        string fullName,
        string timeZoneId,
        string preferredCefrLevel)
    {
        return UserEntity.Create(
            username,
            $"{username}@example.com",
            fullName,
            "hash",
            preferredCefrLevel: preferredCefrLevel,
            timeZoneId: timeZoneId);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }
}
