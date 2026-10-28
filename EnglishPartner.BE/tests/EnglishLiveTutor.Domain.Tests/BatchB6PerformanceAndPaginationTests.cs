using System.Diagnostics;
using EnglishLiveTutor.Application.Common.Behaviors;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Domain.Topics;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Repositories;
using EnglishLiveTutor.Infrastructure.VocabularyGames;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class BatchB6PerformanceAndPaginationTests
{
    private static DbContextOptions<AppDbContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
    }

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

    private sealed class SimpleTestRequest : IRequest<string>
    {
    }

    private sealed class FailingTestRequest : IRequest<string>
    {
    }

    private sealed class MemoryLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception), exception));
        }
    }

    #region P1 — Pagination Bounding Tests

    [Fact]
    public void FromPage_UnpagedRequest_OnItemsLargerThan1000_Returns1000AndRealTotalCount()
    {
        var rawItems = Enumerable.Range(1, 1250).Select(i => $"Item_{i}").ToList();

        var result = PagedResultDto<string>.FromPage(rawItems, totalCount: 1250, page: null, pageSize: null);

        Assert.Equal(1250, result.TotalCount);
        Assert.Equal(PagedResultDto<string>.MaxUnpagedItems, result.Items.Count);
        Assert.Equal(1000, result.Items.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(1000, result.PageSize);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal("Item_1", result.Items.First());
        Assert.Equal("Item_1000", result.Items.Last());
    }

    [Fact]
    public async Task CreateAsync_UnpagedRequest_OnCollectionLargerThan1000_Returns1000AndRealTotalCount()
    {
        var options = CreateInMemoryOptions();
        await using (var db = new AppDbContext(options))
        {
            for (int i = 1; i <= 1100; i++)
            {
                db.Topics.Add(new TopicEntity(
                    code: $"topic_{i:D4}",
                    name: $"Topic {i}",
                    displayNameVi: $"Chủ đề {i}",
                    category: "General"));
            }
            await db.SaveChangesAsync();
        }

        await using (var db = new AppDbContext(options))
        {
            var result = await PagedResultDto<TopicEntity>.CreateAsync(
                db.Topics.AsNoTracking().OrderBy(t => t.Code),
                page: null,
                pageSize: null);

            Assert.Equal(1100, result.TotalCount);
            Assert.Equal(PagedResultDto<TopicEntity>.MaxUnpagedItems, result.Items.Count);
            Assert.Equal(1000, result.Items.Count);
            Assert.Equal(1, result.Page);
            Assert.Equal(1000, result.PageSize);
            Assert.Equal(2, result.TotalPages);
        }
    }

    [Fact]
    public async Task CreateProjectedAsync_UnpagedRequest_OnCollectionLargerThan1000_BoundsTo1000()
    {
        var options = CreateInMemoryOptions();
        await using (var db = new AppDbContext(options))
        {
            for (int i = 1; i <= 1050; i++)
            {
                db.Topics.Add(new TopicEntity(
                    code: $"topic_{i:D4}",
                    name: $"Topic {i}",
                    displayNameVi: $"Chủ đề {i}",
                    category: "General"));
            }
            await db.SaveChangesAsync();
        }

        await using (var db = new AppDbContext(options))
        {
            var result = await PagedResultDto<TopicEntity>.CreateProjectedAsync(
                db.Topics.AsNoTracking().OrderBy(t => t.Code),
                (topics, _) => Task.FromResult(topics.Select(t => t.Code).ToList()),
                page: null,
                pageSize: null);

            Assert.Equal(1050, result.TotalCount);
            Assert.Equal(1000, result.Items.Count);
            Assert.Equal(1, result.Page);
            Assert.Equal(1000, result.PageSize);
            Assert.Equal(2, result.TotalPages);
        }
    }

    [Fact]
    public async Task CreateAsync_PageSize500_IsClampedTo100()
    {
        var options = CreateInMemoryOptions();
        await using (var db = new AppDbContext(options))
        {
            for (int i = 1; i <= 150; i++)
            {
                db.Topics.Add(new TopicEntity(
                    code: $"topic_{i:D3}",
                    name: $"Topic {i}",
                    displayNameVi: $"Chủ đề {i}",
                    category: "General"));
            }
            await db.SaveChangesAsync();
        }

        await using (var db = new AppDbContext(options))
        {
            var result = await PagedResultDto<TopicEntity>.CreateAsync(
                db.Topics.AsNoTracking().OrderBy(t => t.Code),
                page: 1,
                pageSize: 500);

            Assert.Equal(150, result.TotalCount);
            Assert.Equal(PagedResultDto<TopicEntity>.MaxPageSize, result.PageSize);
            Assert.Equal(100, result.PageSize);
            Assert.Equal(100, result.Items.Count);
            Assert.Equal(2, result.TotalPages);
        }
    }

    [Fact]
    public void FromPage_PageSize500_IsClampedTo100()
    {
        var items = Enumerable.Range(1, 100).Select(i => $"Item_{i}").ToList();

        var result = PagedResultDto<string>.FromPage(items, totalCount: 250, page: 1, pageSize: 500);

        Assert.Equal(PagedResultDto<string>.MaxPageSize, result.PageSize);
        Assert.Equal(100, result.PageSize);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task TopicRepository_ListPagedAsync_ClampsPageSizeAndMaxUnpaged()
    {
        var options = CreateInMemoryOptions();
        var factoryMock = new TestDbContextFactory(options);
        var repo = new TopicRepository(new TestDbContextFactoryWrapper(options));

        await using (var db = new AppDbContext(options))
        {
            for (int i = 1; i <= 1050; i++)
            {
                db.Topics.Add(new TopicEntity(
                    code: $"topic_{i:D4}",
                    name: $"Topic {i}",
                    displayNameVi: $"Chủ đề {i}",
                    category: "General"));
            }
            await db.SaveChangesAsync();
        }

        // Test unpaged: capped at 1000 items, totalCount is 1050
        var (unpagedItems, totalCount) = await repo.ListPagedAsync(
            onlyActive: false,
            search: null,
            page: null,
            pageSize: null);

        Assert.Equal(1050, totalCount);
        Assert.Equal(1000, unpagedItems.Count);

        // Test pageSize=500: clamped to 100 items
        var (pagedItems, _) = await repo.ListPagedAsync(
            onlyActive: false,
            search: null,
            page: 1,
            pageSize: 500);

        Assert.Equal(100, pagedItems.Count);
    }

    private sealed class TestDbContextFactoryWrapper : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public TestDbContextFactoryWrapper(DbContextOptions<AppDbContext> options)
        {
            _options = options;
        }

        public AppDbContext CreateDbContext() => new(_options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken ct = default) =>
            Task.FromResult(new AppDbContext(_options));
    }

    #endregion

    #region P3 — Worker Polling Configuration Tests

    [Fact]
    public void FallingWordRoundExpirationWorker_DefaultPollingInterval_Is10Seconds()
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();
        var logger = NullLogger<FallingWordRoundExpirationWorker>.Instance;

        var worker = new FallingWordRoundExpirationWorker(sp, TimeProvider.System, logger);

        Assert.Equal(TimeSpan.FromSeconds(10), worker.PollingInterval);
    }

    [Fact]
    public void FallingWordRoundExpirationWorker_ReadsIntervalFromConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VocabularyGames:ExpirationPollSeconds"] = "25"
            })
            .Build();

        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();
        var logger = NullLogger<FallingWordRoundExpirationWorker>.Instance;

        var worker = new FallingWordRoundExpirationWorker(
            sp,
            TimeProvider.System,
            logger,
            configuration: config);

        Assert.Equal(TimeSpan.FromSeconds(25), worker.PollingInterval);
    }

    [Fact]
    public void FallingWordRoundExpirationWorker_ReadsIntervalFromOptions()
    {
        var options = Options.Create(new VocabularyGamesOptions
        {
            ExpirationPollSeconds = 15
        });

        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();
        var logger = NullLogger<FallingWordRoundExpirationWorker>.Instance;

        var worker = new FallingWordRoundExpirationWorker(
            sp,
            TimeProvider.System,
            logger,
            options: options);

        Assert.Equal(TimeSpan.FromSeconds(15), worker.PollingInterval);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(301)]
    [InlineData(500)]
    public void FallingWordRoundExpirationWorker_ThrowsOnInvalidInterval(int invalidSeconds)
    {
        var options = Options.Create(new VocabularyGamesOptions
        {
            ExpirationPollSeconds = invalidSeconds
        });

        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();
        var logger = NullLogger<FallingWordRoundExpirationWorker>.Instance;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new FallingWordRoundExpirationWorker(sp, TimeProvider.System, logger, options: options));
    }

    #endregion

    #region P3 — LoggingBehavior Level and Exception Privacy Tests

    [Fact]
    public async Task LoggingBehavior_LogsHandlingAndHandledAtDebug()
    {
        var memoryLogger = new MemoryLogger<LoggingBehavior<SimpleTestRequest, string>>();
        var behavior = new LoggingBehavior<SimpleTestRequest, string>(memoryLogger);

        var response = await behavior.Handle(
            new SimpleTestRequest(),
            (token) => Task.FromResult("OK"),
            CancellationToken.None);

        Assert.Equal("OK", response);

        var handlingLog = memoryLogger.Entries.FirstOrDefault(e => e.Message.Contains("Handling SimpleTestRequest"));
        var handledLog = memoryLogger.Entries.FirstOrDefault(e => e.Message.Contains("Handled SimpleTestRequest"));

        Assert.NotNull(handlingLog.Message);
        Assert.Equal(LogLevel.Debug, handlingLog.Level);

        Assert.NotNull(handledLog.Message);
        Assert.Equal(LogLevel.Debug, handledLog.Level);
    }

    [Fact]
    public async Task LoggingBehavior_OnFailure_LogsWarningWithoutExParameter()
    {
        var memoryLogger = new MemoryLogger<LoggingBehavior<FailingTestRequest, string>>();
        var behavior = new LoggingBehavior<FailingTestRequest, string>(memoryLogger);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new FailingTestRequest(),
                (token) => throw new InvalidOperationException("Sensitive URL: https://api.gemini.com?key=SECRET123"),
                CancellationToken.None));

        var warningLog = memoryLogger.Entries.FirstOrDefault(e => e.Level == LogLevel.Warning);
        Assert.NotNull(warningLog.Message);

        // Warning message contains RequestName and ExceptionType
        Assert.Contains("FailingTestRequest", warningLog.Message);
        Assert.Contains(nameof(InvalidOperationException), warningLog.Message);

        // Security check: ex object was NOT passed to LogWarning (so Exception is null)
        Assert.Null(warningLog.Exception);

        // Security check: sensitive message was NOT logged in formatter
        Assert.DoesNotContain("SECRET123", warningLog.Message);
    }

    #endregion

    #region P4 — Normalized Search Tests

    [Fact]
    public async Task TopicRepository_SearchByCode_MatchesCaseInsensitively()
    {
        var options = CreateInMemoryOptions();
        var repo = new TopicRepository(new TestDbContextFactoryWrapper(options));

        await using (var db = new AppDbContext(options))
        {
            db.Topics.Add(new TopicEntity(
                code: "ielts_speaking",
                name: "IELTS Speaking",
                displayNameVi: "Luyện thi IELTS Speaking",
                category: "Exam"));
            await db.SaveChangesAsync();
        }

        // Search with UPPERCASE "IELTS" should match lowercase code "ielts_speaking"
        var (items, count) = await repo.ListPagedAsync(
            onlyActive: false,
            search: "IELTS");

        Assert.Equal(1, count);
        Assert.Single(items);
        Assert.Equal("ielts_speaking", items[0].Code);
    }

    [Fact]
    public async Task ListRolesQuery_SearchByCode_MatchesCaseInsensitively()
    {
        var options = CreateInMemoryOptions();
        var factory = new TestDbContextFactory(options);
        var handler = new EnglishLiveTutor.Application.Features.Identity.Queries.ListRoles.ListRolesQueryHandler(factory);

        await using (var db = new AppDbContext(options))
        {
            db.Roles.Add(new RoleEntity(
                code: "ADMIN",
                name: "Administrator",
                description: "Quản trị viên"));
            await db.SaveChangesAsync();
        }

        // Search with lowercase "admin" should match uppercase code "ADMIN"
        var result = await handler.HandleAsync(new EnglishLiveTutor.Application.Features.Identity.Queries.ListRoles.ListRolesQuery(
            Search: "admin"));

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("ADMIN", result.Items[0].Code);
    }

    #endregion
}
