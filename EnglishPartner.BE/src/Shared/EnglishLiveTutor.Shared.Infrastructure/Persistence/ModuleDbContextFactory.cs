using EnglishLiveTutor.Application.Modules;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Persistence;

public sealed class ModuleDbContextFactory<TDb, TContext> : IModuleDbContextFactory<TDb>
    where TDb : class, IModuleDbContext
    where TContext : DbContext, TDb
{
    private readonly IDbContextFactory<TContext> _contextFactory;

    public ModuleDbContextFactory(IDbContextFactory<TContext> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
    }

    public async Task<TDb> CreateDbContextAsync(CancellationToken ct = default)
    {
        return await _contextFactory.CreateDbContextAsync(ct);
    }
}
