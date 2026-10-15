using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Domain.Prompts;
using EnglishLiveTutor.Domain.Topics;
using EnglishLiveTutor.Domain.Voices;
using EnglishLiveTutor.Infrastructure.Ai;
using EnglishLiveTutor.Infrastructure.Identity;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class IdentityPromptsAndVoicesTests
{
    [Fact]
    public void UserAggregate_ShouldManageRoles_AndRotateOrRevokeRefreshTokens()
    {
        var hasher = new Pbkdf2PasswordHasher();
        string hash = hasher.HashPassword("StrongPass#2026");

        Assert.True(hasher.VerifyPassword("StrongPass#2026", hash));
        Assert.False(hasher.VerifyPassword("WrongPass", hash));

        var user = UserEntity.Create("alice", "Alice@Example.com", "Alice Nguyen", hash);
        Assert.Equal("ALICE@EXAMPLE.COM", user.NormalizedEmail);

        var roleId = Guid.NewGuid();
        user.AssignRole(roleId);
        user.AssignRole(roleId); // Idempotent
        Assert.Single(user.UserRoles);

        var now = DateTimeOffset.UtcNow;
        var rt1 = user.IssueRefreshToken("token-1", now.AddDays(7), "127.0.0.1", "Chrome");
        Assert.True(rt1.IsActive(now));

        var rt2 = user.RotateRefreshToken("token-1", "token-2", now.AddDays(14), now.AddMinutes(5), "127.0.0.1", "Chrome");
        Assert.True(rt1.IsRevoked);
        Assert.Equal("token-2", rt1.ReplacedByToken);
        Assert.True(rt2.IsActive(now.AddMinutes(5)));

        user.RevokeRefreshToken("token-2", now.AddMinutes(10), "User logout");
        Assert.False(rt2.IsActive(now.AddMinutes(10)));
    }

    [Fact]
    public void JwtTokenService_ShouldGenerateValidJwtAndRefreshToken()
    {
        var jwtService = new HmacJwtTokenService();
        var user = UserEntity.Create("bob", "bob@lingomint.local", "Bob Tran", "hash");

        var tokenResult = jwtService.GenerateAccessToken(
            user,
            new[] { "ADMIN", "TEACHER" },
            new[] { "users.manage", "prompts.manage", "topics.manage" });

        Assert.False(string.IsNullOrWhiteSpace(tokenResult.AccessToken));
        Assert.Equal(3, tokenResult.AccessToken.Split('.').Length);

        var principal = jwtService.ValidateAccessToken(tokenResult.AccessToken);
        Assert.NotNull(principal);
        Assert.True(principal!.Identity?.IsAuthenticated);
        Assert.Contains(principal.Claims, c => c.Type == "permission" && c.Value == "topics.manage");

        string refreshToken = jwtService.GenerateRefreshTokenString();
        Assert.False(string.IsNullOrWhiteSpace(refreshToken));
    }

    [Fact]
    public async Task CefrPromptBuilder_ShouldLoadCustomCefrAndModePromptsFromDatabase()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var currentUser = new FakeCurrentUser { UserId = "system" };
        var interceptor = new AuditingSaveChangesInterceptor(clock, currentUser);
        string dbName = Guid.NewGuid().ToString("N");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(interceptor)
            .Options;

        await using var db = new AppDbContext(options);
        db.CefrLevelPrompts.AddRange(CefrPromptBuilder.GetDefaultCefrLevelPrompts());
        db.ConversationModePrompts.AddRange(CefrPromptBuilder.GetDefaultConversationModePrompts());
        await db.SaveChangesAsync();

        Assert.Equal(6, await db.CefrLevelPrompts.CountAsync());
        Assert.Equal(19, await db.ConversationModePrompts.CountAsync());

        // Customize B2 level guidance in DB
        var b2 = await db.CefrLevelPrompts.FirstAsync(x => x.CefrLevel == "B2");
        b2.UpdateGuidance("Custom B2", "CUSTOM_DB_B2_GUIDANCE_MARKER", 4);
        await db.SaveChangesAsync();

        var factory = new SingleOptionsDbContextFactory(options);
        var builder = new CefrPromptBuilder(factory);

        string prompt = await builder.BuildSystemInstructionAsync(
            cefrLevel: "B2",
            extendedThinking: true,
            voiceName: "Aoede",
            speechSpeed: 1.0,
            conversationMode: "english_coach",
            topic: "Clean Architecture & DDD");

        Assert.Contains("CUSTOM_DB_B2_GUIDANCE_MARKER", prompt);
        Assert.Contains("trình độ B2", prompt);
        Assert.Contains("Clean Architecture & DDD", prompt);
    }

    [Fact]
    public void TeacherVoicesCatalog_ShouldContainAll30GooglePrebuiltVoices_With6FeaturedVoices()
    {
        var voices = DatabaseSeeder.BuildDefaultTeacherVoices();

        Assert.Equal(30, voices.Count);

        var featured = voices.Where(v => v.IsFeatured).Select(v => v.Id).ToHashSet();
        Assert.Equal(6, featured.Count);
        Assert.Contains("Aoede", featured);
        Assert.Contains("Puck", featured);
        Assert.Contains("Kore", featured);
        Assert.Contains("Charon", featured);
        Assert.Contains("Fenrir", featured);
        Assert.Contains("Leda", featured);

        var aoede = voices.First(v => v.Id == "Aoede");
        Assert.Equal("Aoede (Nữ)", aoede.Label);
        Assert.Equal("Truyền cảm, ấm áp & rõ chữ", aoede.Style);

        var sulafat = voices.First(v => v.Id == "Sulafat");
        Assert.Equal("Female", sulafat.Gender);
        Assert.Equal("Warm", sulafat.Persona);
    }

    [Fact]
    public void TopicsAggregateCatalog_ShouldContain26CuratedTopics_ForConversationAndVocabulary()
    {
        var topics = DatabaseSeeder.BuildDefaultTopics();
        Assert.Equal(26, topics.Count);
        Assert.Contains(topics, t => t.Code == "net10_clean_arch_ddd" && t.SupportsConversation && t.SupportsVocabulary);
        Assert.Contains(topics, t => t.Code == "sysdesign_payment_gateway" && t.ModeCode == "system_design");
        Assert.Contains(topics, t => t.Code == "vocab_business_idioms_phrasal_verbs" && t.SupportsVocabulary);
    }

    private sealed class SingleOptionsDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;
        public SingleOptionsDbContextFactory(DbContextOptions<AppDbContext> options) => _options = options;
        public AppDbContext CreateDbContext() => new(_options);
    }
}
