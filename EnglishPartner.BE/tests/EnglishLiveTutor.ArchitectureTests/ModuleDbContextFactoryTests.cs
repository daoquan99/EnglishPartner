using EnglishLiveTutor.Application.Modules;
using EnglishLiveTutor.Application.Modules.Vocabulary;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public sealed class ModuleDbContextFactoryTests
{
    [Fact]
    public async Task CreateDbContextAsync_ImplementedModuleInterface_ReturnsTypedContext()
    {
        using var provider = CreateProvider();
        var dbFactory = provider.GetRequiredService<IDbContextFactory<VocabularyDbContext>>();
        var factory = new ModuleDbContextFactory<IVocabularyDbContext, VocabularyDbContext>(dbFactory);

        await using var context = await factory.CreateDbContextAsync();

        Assert.IsType<VocabularyDbContext>(context);
        Assert.IsAssignableFrom<IVocabularyDbContext>(context);
    }

    [Fact]
    public void Constructor_NullContextFactory_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => new ModuleDbContextFactory<IVocabularyDbContext, VocabularyDbContext>(null!));

        Assert.Equal("contextFactory", exception.ParamName);
    }

    [Fact]
    public async Task CreateDbContextAsync_FactoryThrowsException_PropagatesException()
    {
        var throwingFactory = new ThrowingDbContextFactory<VocabularyDbContext>(
            new InvalidOperationException("Factory creation failed."));
        var factory = new ModuleDbContextFactory<IVocabularyDbContext, VocabularyDbContext>(throwingFactory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => factory.CreateDbContextAsync());

        Assert.Equal("Factory creation failed.", ex.Message);
    }

    [Fact]
    public async Task CreateDbContextAsync_PassesCancellationTokenToUnderlyingFactory()
    {
        var trackingFactory = new TokenTrackingDbContextFactory<VocabularyDbContext>();
        var factory = new ModuleDbContextFactory<IVocabularyDbContext, VocabularyDbContext>(trackingFactory);

        using var cts = new CancellationTokenSource();
        await factory.CreateDbContextAsync(cts.Token);

        Assert.Equal(cts.Token, trackingFactory.ObservedToken);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<VocabularyDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));
        return services.BuildServiceProvider();
    }

    private sealed class ThrowingDbContextFactory<TContext>(Exception exception) : IDbContextFactory<TContext>
        where TContext : DbContext
    {
        public TContext CreateDbContext() => throw exception;

        public Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromException<TContext>(exception);
    }

    private sealed class TokenTrackingDbContextFactory<TContext> : IDbContextFactory<TContext>
        where TContext : DbContext
    {
        public CancellationToken ObservedToken { get; private set; }

        public TContext CreateDbContext() => throw new NotImplementedException();

        public Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            ObservedToken = cancellationToken;
            return Task.FromResult<TContext>(null!);
        }
    }
}
