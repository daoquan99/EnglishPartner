using EnglishLiveTutor.Application.Modules;

namespace EnglishLiveTutor.Domain.Tests.TestDoubles;

public sealed class ModuleDbContextFactoryStub<TDb>(Func<TDb> create)
    : IModuleDbContextFactory<TDb>
    where TDb : IModuleDbContext
{
    public Task<TDb> CreateDbContextAsync(CancellationToken ct = default) =>
        Task.FromResult(create());
}
