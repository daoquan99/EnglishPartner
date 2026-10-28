using System.Security.Claims;
using System.Text.Encodings.Web;
using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Identity.Commands.AdminCreateUser;
using EnglishLiveTutor.Application.Features.Identity.Commands.Login;
using EnglishLiveTutor.Application.Features.Identity.Commands.RefreshToken;
using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Domain.Identity.Entities;
using EnglishLiveTutor.Infrastructure.Identity;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class AuthBatchB2SecurityTests
{
    private sealed class TestDbContextFactory : IAppDbContextFactory
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public TestDbContextFactory(DbContextOptions<AppDbContext> options)
        {
            _options = options;
        }

        public Task<IAppDbContext> CreateDbContextAsync(CancellationToken ct = default)
        {
            return Task.FromResult<IAppDbContext>(new AppDbContext(_options));
        }
    }

    private static DbContextOptions<AppDbContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
    }

    [Fact]
    public void RefreshTokenHash_ShouldBeLowerHexSha256_64Characters()
    {
        string rawToken = "raw-random-token-xyz-123456789";
        string hash = RefreshTokenEntity.HashToken(rawToken);

        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-f]{64}$", hash);
        Assert.Equal(hash, RefreshTokenEntity.HashToken(rawToken)); // Deterministic
    }

    [Fact]
    public async Task Login_ShouldStoreTokenHashInDatabase_NotRawToken()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);

        var hasher = new Pbkdf2PasswordHasher();
        string password = "StrongPassword#2026";
        var user = UserEntity.Create("bob", "bob@example.com", "Bob Test", hasher.HashPassword(password));
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var userRepo = new UserRepository(db);
        var dbFactory = new TestDbContextFactory(options);
        var jwtService = new HmacJwtTokenService(new JwtOptions { SecretKey = "TestSecretKeyAtLeast32BytesLongForTesting!" });

        var handler = new LoginCommandHandler(userRepo, db, new UnitOfWork(db), hasher, jwtService);
        var response = await handler.HandleAsync(new LoginCommand(new LoginRequest("bob", password), "127.0.0.1"));

        Assert.NotNull(response.RefreshToken);
        string rawRefreshToken = response.RefreshToken;
        string expectedHash = RefreshTokenEntity.HashToken(rawRefreshToken);

        // Kiểm tra trong DB: Raw token tuyệt đối KHÔNG có trong DB, chỉ có Hash
        var tokenInDb = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.UserId == user.Id);
        Assert.NotNull(tokenInDb);
        Assert.Equal(expectedHash, tokenInDb!.Token);
        Assert.NotEqual(rawRefreshToken, tokenInDb.Token);
        Assert.False(await db.RefreshTokens.AnyAsync(rt => rt.Token == rawRefreshToken));
    }

    [Fact]
    public async Task RefreshTokenReuse_ShouldRevokeAllActiveTokensOfUser()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);

        var hasher = new Pbkdf2PasswordHasher();
        var user = UserEntity.Create("carol", "carol@example.com", "Carol Test", hasher.HashPassword("Pass123456"));
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var userRepo = new UserRepository(db);
        var dbFactory = new TestDbContextFactory(options);
        var jwtService = new HmacJwtTokenService(new JwtOptions { SecretKey = "TestSecretKeyAtLeast32BytesLongForTesting!" });

        // 1. Cấp token 1
        var loginHandler = new LoginCommandHandler(userRepo, db, new UnitOfWork(db), hasher, jwtService);
        var loginRes = await loginHandler.HandleAsync(new LoginCommand(new LoginRequest("carol", "Pass123456"), "127.0.0.1"));
        string rawToken1 = loginRes.RefreshToken;

        // 2. Rotate token 1 -> token 2 thành công
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var refreshHandler = new RefreshTokenCommandHandler(userRepo, db, new UnitOfWork(db), jwtService, timeProvider: clock);
        var refreshRes1 = await refreshHandler.HandleAsync(new RefreshTokenCommand(new RefreshTokenRequest(rawToken1), "127.0.0.1"));
        string rawToken2 = refreshRes1.RefreshToken;
        Assert.NotEqual(rawToken1, rawToken2);

        // Token 2 đang active
        string token2Hash = RefreshTokenEntity.HashToken(rawToken2);
        var token2InDb = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token2Hash);
        Assert.NotNull(token2InDb);
        Assert.False(token2InDb!.IsRevoked);

        // 3. Tái sử dụng token 1 cũ đã rotate ngoài khoảng ân hạn (15s > 10s) -> REUSE DETECTED!
        clock.Advance(TimeSpan.FromSeconds(15));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            refreshHandler.HandleAsync(new RefreshTokenCommand(new RefreshTokenRequest(rawToken1), "127.0.0.1")));
        Assert.Equal("RefreshToken không hợp lệ.", ex.Message);

        // 4. Kiểm tra trong DB: TOÀN BỘ token active của user (bao gồm token 2) phải bị thu hồi với reason "Reuse detected"
        var activeTokens = await db.RefreshTokens.Where(rt => rt.UserId == user.Id && rt.RevokedAtUtc == null).ToListAsync();
        Assert.Empty(activeTokens);

        var token2AfterReuse = await db.RefreshTokens.FirstAsync(rt => rt.Token == token2Hash);
        Assert.True(token2AfterReuse.IsRevoked);
        Assert.Equal("Reuse detected", token2AfterReuse.RevokedReason);

        // 5. Thử dùng token 2 tiếp theo -> cũng phải bị từ chối
        var ex2 = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            refreshHandler.HandleAsync(new RefreshTokenCommand(new RefreshTokenRequest(rawToken2), "127.0.0.1")));
        Assert.Equal("RefreshToken không hợp lệ.", ex2.Message);
    }

    [Fact]
    public async Task AuthenticationHandler_AccessTokenInQuery_AcceptedOnlyForHubsPath()
    {
        var jwtService = new HmacJwtTokenService(new JwtOptions { SecretKey = "TestSecretKeyAtLeast32BytesLongForTesting!" });
        var user = UserEntity.Create("david", "david@example.com", "David", "hash");
        var tokenResult = jwtService.GenerateAccessToken(user, new[] { "LEARNER" }, new[] { "conversations.access" });
        string validToken = tokenResult.AccessToken;

        var optionsMonitor = new TestOptionsMonitor<AuthenticationSchemeOptions>(new AuthenticationSchemeOptions());
        var handler = new EltAuthenticationHandler(
            optionsMonitor,
            NullLoggerFactory.Instance,
            UrlEncoder.Default,
            jwtService);

        // Test Case 1: Query token on normal API endpoint (/api/users) -> Phải bị BỎ QUA
        var httpContextApi = new DefaultHttpContext();
        httpContextApi.Request.Path = "/api/users";
        httpContextApi.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues> { ["access_token"] = validToken });
        await handler.InitializeAsync(new AuthenticationScheme("Bearer", null, typeof(EltAuthenticationHandler)), httpContextApi);

        var resultApi = await handler.AuthenticateAsync();
        Assert.False(resultApi.Succeeded);
        Assert.True(resultApi.None);

        // Test Case 2: Query token on SignalR Hubs (/hubs/tutor) -> Được CHẤP NHẬN
        var httpContextHub = new DefaultHttpContext();
        httpContextHub.Request.Path = "/hubs/tutor";
        httpContextHub.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues> { ["access_token"] = validToken });
        var handler2 = new EltAuthenticationHandler(optionsMonitor, NullLoggerFactory.Instance, UrlEncoder.Default, jwtService);
        await handler2.InitializeAsync(new AuthenticationScheme("Bearer", null, typeof(EltAuthenticationHandler)), httpContextHub);

        var resultHub = await handler2.AuthenticateAsync();
        Assert.True(resultHub.Succeeded);
        Assert.NotNull(resultHub.Principal);
        Assert.Equal(user.Id.ToString(), resultHub.Principal!.FindFirstValue(ClaimTypes.NameIdentifier));
    }

    [Fact]
    public void HasPermission_ShouldCorrectlyEnforceAdminBypassAndUserPermissions()
    {
        // 1. ADMIN bypass qua ClaimTypes.Role
        var adminClaimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "ADMIN")
        }, "Bearer"));
        Assert.True(adminClaimsPrincipal.HasPermission("any.random.permission"));
        Assert.True(adminClaimsPrincipal.HasPermission("roles.manage"));

        // 2. ADMIN bypass qua claim role chữ thường
        var adminLowerPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim("role", "admin")
        }, "Bearer"));
        Assert.True(adminLowerPrincipal.HasPermission("lessons.delete"));

        // 3. User thường có permission
        var learnerWithPerm = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "LEARNER"),
            new Claim("permission", "vocabularies.generate")
        }, "Bearer"));
        Assert.True(learnerWithPerm.HasPermission("vocabularies.generate"));
        Assert.False(learnerWithPerm.HasPermission("users.manage"));

        // 4. User thường không có permission
        var learnerWithoutPerm = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "LEARNER")
        }, "Bearer"));
        Assert.False(learnerWithoutPerm.HasPermission("roles.manage"));

        // 5. User unauthenticated
        var unauthenticatedPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
        Assert.False(unauthenticatedPrincipal.HasPermission("any.permission"));

        ClaimsPrincipal? nullPrincipal = null;
        Assert.False(nullPrincipal.HasPermission("any.permission"));
    }

    [Fact]
    public void AdminCreateUserValidator_RequiresPasswordAtLeast8Characters()
    {
        var validator = new AdminCreateUserValidator();

        var emptyPasswordReq = new AdminCreateUserRequest("newuser", "new@example.com", "", "Full Name");
        var result1 = validator.Validate(new AdminCreateUserCommand(emptyPasswordReq));
        Assert.False(result1.IsValid);
        Assert.Contains(result1.Errors, e => e.PropertyName.Contains("Password"));

        var shortPasswordReq = new AdminCreateUserRequest("newuser", "new@example.com", "1234567", "Full Name");
        var result2 = validator.Validate(new AdminCreateUserCommand(shortPasswordReq));
        Assert.False(result2.IsValid);
        Assert.Contains(result2.Errors, e => e.PropertyName.Contains("Password"));

        var validPasswordReq = new AdminCreateUserRequest("newuser", "new@example.com", "Password#123", "Full Name");
        var result3 = validator.Validate(new AdminCreateUserCommand(validPasswordReq));
        Assert.True(result3.IsValid);
    }

    private sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public TestOptionsMonitor(T currentValue) => CurrentValue = currentValue;
        public T CurrentValue { get; }
        public T Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
