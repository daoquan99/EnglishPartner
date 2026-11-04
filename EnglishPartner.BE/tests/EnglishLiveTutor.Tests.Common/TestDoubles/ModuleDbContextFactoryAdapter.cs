using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Modules;

namespace EnglishLiveTutor.Domain.Tests.TestDoubles;

public sealed class ModuleDbContextFactoryAdapter<TDb>(IAppDbContextFactory inner)
    : IModuleDbContextFactory<TDb>
    where TDb : IModuleDbContext
{
    public async Task<TDb> CreateDbContextAsync(CancellationToken ct = default)
    {
        return (TDb)await inner.CreateDbContextAsync(ct);
    }
}
