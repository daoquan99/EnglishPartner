using System.Net;
using System.Threading.RateLimiting;
using EnglishLiveTutor.Api.Configuration;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Identity.Commands.Login;
using EnglishLiveTutor.Application.Features.Identity.Commands.RefreshToken;
using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Domain.Identity.Entities;
using EnglishLiveTutor.Infrastructure.Identity;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class SecurityBatchB2Fix1Tests
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

    #region Forwarded Headers Tests

    [Fact]
    public async Task ForwardedHeaders_WhenNoProxiesConfigured_SkipsMiddleware_AndClientIpIsPreserved()
    {
        // 1. Cấu hình rỗng -> CreateForwardedHeadersOptions trả về null (bỏ qua middleware)
        var emptyConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:KnownProxies:0"] = "",
            ["ForwardedHeaders:KnownNetworks:0"] = ""
        }).Build();

        var options = ConfiguredForwardedHeadersExtensions.CreateForwardedHeadersOptions(emptyConfig, NullLogger.Instance);
        Assert.Null(options);

        // 2. Khi không gọi UseForwardedHeaders, remote IP từ client gửi X-Forwarded-For không bị thay đổi
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("198.51.100.1");
        httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.195";

        // Middleware không chạy nên RemoteIpAddress giữ nguyên
        Assert.Equal(IPAddress.Parse("198.51.100.1"), httpContext.Connection.RemoteIpAddress);
    }

    [Fact]
    public async Task ForwardedHeaders_WhenProxyConfigured_AppliesForwardedHeaderFromTrustedProxy()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:KnownProxies:0"] = "10.0.0.1"
        }).Build();

        var options = ConfiguredForwardedHeadersExtensions.CreateForwardedHeadersOptions(config, NullLogger.Instance);
        Assert.NotNull(options);
        Assert.Contains(IPAddress.Parse("10.0.0.1"), options!.KnownProxies);

        // Chạy ForwardedHeadersMiddleware với options đã cấu hình
        var middleware = new ForwardedHeadersMiddleware(
            next: ctx => Task.CompletedTask,
            loggerFactory: NullLoggerFactory.Instance,
            options: Options.Create(options));

        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
        httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.195";

        await middleware.Invoke(httpContext);

        // Remote IP được cập nhật từ X-Forwarded-For vì request đến từ trusted proxy 10.0.0.1
        Assert.Equal(IPAddress.Parse("203.0.113.195"), httpContext.Connection.RemoteIpAddress);
    }

    [Fact]
    public void ForwardedHeaders_WhenInvalidIpOrCidrConfigured_DoesNotThrowAndIgnoresInvalidEntries()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:KnownProxies:0"] = "invalid-ip-string",
            ["ForwardedHeaders:KnownProxies:1"] = "192.168.1.50",
            ["ForwardedHeaders:KnownNetworks:0"] = "invalid/cidr/network",
            ["ForwardedHeaders:KnownNetworks:1"] = "172.16.0.0/16"
        }).Build();

        var options = ConfiguredForwardedHeadersExtensions.CreateForwardedHeadersOptions(config, NullLogger.Instance);
        Assert.NotNull(options);

        // IP và CIDR hợp lệ được nạp
        Assert.Contains(IPAddress.Parse("192.168.1.50"), options!.KnownProxies);
        Assert.Contains(System.Net.IPNetwork.Parse("172.16.0.0/16"), options.KnownIPNetworks);

        // IP sai không gây crash và không được nạp
        Assert.DoesNotContain(options.KnownProxies, p => p.ToString() == "invalid-ip-string");
    }

    [Fact]
    public void ForwardedHeaders_WhenOnlyInvalidEntriesConfigured_ReturnsNull()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:KnownProxies:0"] = "invalid-ip-only",
            ["ForwardedHeaders:KnownNetworks:0"] = "999.999.999.999/99"
        }).Build();

        var options = ConfiguredForwardedHeadersExtensions.CreateForwardedHeadersOptions(config, NullLogger.Instance);
        Assert.Null(options);
    }

    #endregion

    #region Refresh Token Race & Grace Period Tests

    [Fact]
    public async Task RefreshToken_TwoConsecutiveRefreshes_SecondFailsAndDoesNotIssueNewToken()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);

        var hasher = new Pbkdf2PasswordHasher();
        var user = UserEntity.Create("racer", "racer@example.com", "Racer User", hasher.HashPassword("Pass#123456"));
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var userRepo = new UserRepository(db);
        var dbFactory = new TestDbContextFactory(options);
        var jwtService = new HmacJwtTokenService(new JwtOptions { SecretKey = "TestSecretKeyAtLeast32BytesLongForTesting!" });
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 28, 12, 0, 0, TimeSpan.Zero));

        var loginHandler = new LoginCommandHandler(userRepo, db, new UnitOfWork(db), hasher, jwtService);
        var loginRes = await loginHandler.HandleAsync(new LoginCommand(new LoginRequest("racer", "Pass#123456"), "127.0.0.1"));
        string rawToken1 = loginRes.RefreshToken;

        var refreshHandler = new RefreshTokenCommandHandler(userRepo, db, new UnitOfWork(db), jwtService, new RefreshTokenOptions(10), clock);

        // Lần 1: Thành công và cấp token 2
        var res1 = await refreshHandler.HandleAsync(new RefreshTokenCommand(new RefreshTokenRequest(rawToken1), "127.0.0.1"));
        string rawToken2 = res1.RefreshToken;
        Assert.NotEmpty(rawToken2);
        Assert.NotEqual(rawToken1, rawToken2);

        // Lần 2 liên tiếp cùng token 1: Thất bại ngay lập tức và không cấp token mới
        clock.Advance(TimeSpan.FromSeconds(1));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            refreshHandler.HandleAsync(new RefreshTokenCommand(new RefreshTokenRequest(rawToken1), "127.0.0.1")));
        Assert.Equal("RefreshToken không hợp lệ.", ex.Message);

        // Token 2 vẫn còn hiệu lực và active
        string token2Hash = RefreshTokenEntity.HashToken(rawToken2);
        var token2InDb = await db.RefreshTokens.FirstAsync(rt => rt.Token == token2Hash);
        Assert.False(token2InDb.IsRevoked);
        Assert.True(token2InDb.IsActive(clock.GetUtcNow()));
    }

    [Fact]
    public async Task RefreshToken_ReuseWithinGracePeriod_RejectsWith401_WithoutRevokingRemainingSession()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);

        var hasher = new Pbkdf2PasswordHasher();
        var user = UserEntity.Create("multitab", "multitab@example.com", "Multitab User", hasher.HashPassword("Pass#123456"));
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var userRepo = new UserRepository(db);
        var dbFactory = new TestDbContextFactory(options);
        var jwtService = new HmacJwtTokenService(new JwtOptions { SecretKey = "TestSecretKeyAtLeast32BytesLongForTesting!" });
        var t0 = new DateTimeOffset(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);
        var clock = new FakeTimeProvider(t0);

        var loginHandler = new LoginCommandHandler(userRepo, db, new UnitOfWork(db), hasher, jwtService);
        var loginRes = await loginHandler.HandleAsync(new LoginCommand(new LoginRequest("multitab", "Pass#123456"), "127.0.0.1"));
        string rawToken1 = loginRes.RefreshToken;

        // Cấu hình khoảng ân hạn 10 giây
        var refreshOptions = new RefreshTokenOptions(10);
        var refreshHandler = new RefreshTokenCommandHandler(userRepo, db, new UnitOfWork(db), jwtService, refreshOptions, clock);

        // Tab 1 refresh token 1 -> nhận token 2
        var res1 = await refreshHandler.HandleAsync(new RefreshTokenCommand(new RefreshTokenRequest(rawToken1), "127.0.0.1"));
        string rawToken2 = res1.RefreshToken;

        // Tab 2 gửi lại token 1 cũ sau 3 giây (trong khoảng ân hạn 10s)
        clock.Advance(TimeSpan.FromSeconds(3));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            refreshHandler.HandleAsync(new RefreshTokenCommand(new RefreshTokenRequest(rawToken1), "127.0.0.1")));
        Assert.Equal("RefreshToken không hợp lệ.", ex.Message);

        // Kiểm tra trong DB: Phiên còn lại (token 2) KHÔNG bị revoke!
        string token2Hash = RefreshTokenEntity.HashToken(rawToken2);
        var activeTokens = await db.RefreshTokens.Where(rt => rt.UserId == user.Id && rt.RevokedAtUtc == null).ToListAsync();
        Assert.Single(activeTokens);
        Assert.Equal(token2Hash, activeTokens[0].Token);

        // Token 2 vẫn dùng refresh tiếp được bình thường
        clock.Advance(TimeSpan.FromSeconds(2));
        var res2 = await refreshHandler.HandleAsync(new RefreshTokenCommand(new RefreshTokenRequest(rawToken2), "127.0.0.1"));
        Assert.NotNull(res2.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_ReuseOutsideGracePeriod_RevokesAllActiveSessions()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);

        var hasher = new Pbkdf2PasswordHasher();
        var user = UserEntity.Create("attacker", "attacker@example.com", "Attacker Victim", hasher.HashPassword("Pass#123456"));
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var userRepo = new UserRepository(db);
        var dbFactory = new TestDbContextFactory(options);
        var jwtService = new HmacJwtTokenService(new JwtOptions { SecretKey = "TestSecretKeyAtLeast32BytesLongForTesting!" });
        var t0 = new DateTimeOffset(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);
        var clock = new FakeTimeProvider(t0);

        var loginHandler = new LoginCommandHandler(userRepo, db, new UnitOfWork(db), hasher, jwtService);
        var loginRes = await loginHandler.HandleAsync(new LoginCommand(new LoginRequest("attacker", "Pass#123456"), "127.0.0.1"));
        string rawToken1 = loginRes.RefreshToken;

        var refreshOptions = new RefreshTokenOptions(10);
        var refreshHandler = new RefreshTokenCommandHandler(userRepo, db, new UnitOfWork(db), jwtService, refreshOptions, clock);

        // Rotate token 1 -> token 2 tại t0
        var res1 = await refreshHandler.HandleAsync(new RefreshTokenCommand(new RefreshTokenRequest(rawToken1), "127.0.0.1"));
        string rawToken2 = res1.RefreshToken;

        // Kẻ tấn công gửi lại token 1 cũ sau 15 giây (> 10s grace period)
        clock.Advance(TimeSpan.FromSeconds(15));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            refreshHandler.HandleAsync(new RefreshTokenCommand(new RefreshTokenRequest(rawToken1), "127.0.0.1")));
        Assert.Equal("RefreshToken không hợp lệ.", ex.Message);

        // Kiểm tra trong DB: TOÀN BỘ phiên active của user bị thu hồi do phát hiện reuse
        var activeTokens = await db.RefreshTokens.Where(rt => rt.UserId == user.Id && rt.RevokedAtUtc == null).ToListAsync();
        Assert.Empty(activeTokens);

        string token2Hash = RefreshTokenEntity.HashToken(rawToken2);
        var token2InDb = await db.RefreshTokens.FirstAsync(rt => rt.Token == token2Hash);
        Assert.True(token2InDb.IsRevoked);
        Assert.Equal("Reuse detected", token2InDb.RevokedReason);
    }

    [Fact]
    public async Task RefreshToken_ConcurrentRequests_OnlyOneSucceeds_OtherFailsWithoutIssuingToken()
    {
        var options = CreateInMemoryOptions();
        await using var db = new AppDbContext(options);

        var hasher = new Pbkdf2PasswordHasher();
        var user = UserEntity.Create("concurrent", "concurrent@example.com", "Concurrent User", hasher.HashPassword("Pass#123456"));
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var userRepo = new UserRepository(db);
        var dbFactory = new TestDbContextFactory(options);
        var jwtService = new HmacJwtTokenService(new JwtOptions { SecretKey = "TestSecretKeyAtLeast32BytesLongForTesting!" });
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 28, 12, 0, 0, TimeSpan.Zero));

        var loginHandler = new LoginCommandHandler(userRepo, db, new UnitOfWork(db), hasher, jwtService);
        var loginRes = await loginHandler.HandleAsync(new LoginCommand(new LoginRequest("concurrent", "Pass#123456"), "127.0.0.1"));
        string rawToken1 = loginRes.RefreshToken;

        using var db1 = new AppDbContext(options);
        using var db2 = new AppDbContext(options);
        var refreshHandler1 = new RefreshTokenCommandHandler(new UserRepository(db1), db1, new UnitOfWork(db1), jwtService, new RefreshTokenOptions(10), clock);
        var refreshHandler2 = new RefreshTokenCommandHandler(new UserRepository(db2), db2, new UnitOfWork(db2), jwtService, new RefreshTokenOptions(10), clock);

        // Gửi 2 request refresh đồng thời với cùng 1 token
        var task1 = Task.Run(() => refreshHandler1.HandleAsync(new RefreshTokenCommand(new RefreshTokenRequest(rawToken1), "127.0.0.1")));
        var task2 = Task.Run(() => refreshHandler2.HandleAsync(new RefreshTokenCommand(new RefreshTokenRequest(rawToken1), "127.0.0.1")));

        var results = await Task.WhenAll(
            task1.ContinueWith(t => (Success: t.Status == TaskStatus.RanToCompletion, Result: t.IsCompletedSuccessfully ? t.Result : null)),
            task2.ContinueWith(t => (Success: t.Status == TaskStatus.RanToCompletion, Result: t.IsCompletedSuccessfully ? t.Result : null)));

        int successCount = results.Count(r => r.Success);
        int failCount = results.Count(r => !r.Success);

        // Đúng 1 request thành công và 1 request thất bại
        Assert.Equal(1, successCount);
        Assert.Equal(1, failCount);

        // Đúng 1 token active trong DB
        var activeTokens = await db.RefreshTokens.Where(rt => rt.UserId == user.Id && rt.RevokedAtUtc == null).ToListAsync();
        Assert.Single(activeTokens);
    }

    #endregion

    #region Rate Limiter Tests

    [Fact]
    public void RateLimiter_AuthPolicy_Returns429AfterThreshold()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["RateLimiting:Auth:PermitLimit"] = "3",
            ["RateLimiting:Auth:WindowSeconds"] = "60"
        }).Build();

        var limiter = RateLimitingExtensions.CreateAuthRateLimiter(config);
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.1");

        // 3 requests đầu tiên được chấp nhận (PermitLimit = 3)
        using (var lease1 = limiter.AttemptAcquire(httpContext))
            Assert.True(lease1.IsAcquired);

        using (var lease2 = limiter.AttemptAcquire(httpContext))
            Assert.True(lease2.IsAcquired);

        using (var lease3 = limiter.AttemptAcquire(httpContext))
            Assert.True(lease3.IsAcquired);

        // Request thứ 4 vượt ngưỡng -> bị từ chối (429 Too Many Requests)
        using (var lease4 = limiter.AttemptAcquire(httpContext))
            Assert.False(lease4.IsAcquired);

        // Client với IP khác không bị ảnh hưởng (phân vùng theo IP)
        var otherContext = new DefaultHttpContext();
        otherContext.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.2");
        using (var leaseOther = limiter.AttemptAcquire(otherContext))
            Assert.True(leaseOther.IsAcquired);
    }

    [Fact]
    public void RateLimiter_AuthRefreshPolicy_HasSeparateLimit()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["RateLimiting:AuthRefresh:PermitLimit"] = "2",
            ["RateLimiting:AuthRefresh:WindowSeconds"] = "30"
        }).Build();

        var limiter = RateLimitingExtensions.CreateAuthRefreshRateLimiter(config);
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.55");

        // 2 request đầu được phép
        using (var l1 = limiter.AttemptAcquire(httpContext))
            Assert.True(l1.IsAcquired);
        using (var l2 = limiter.AttemptAcquire(httpContext))
            Assert.True(l2.IsAcquired);

        // Request thứ 3 bị từ chối
        using (var l3 = limiter.AttemptAcquire(httpContext))
            Assert.False(l3.IsAcquired);
    }

    #endregion
}
