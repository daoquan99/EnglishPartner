namespace EnglishLiveTutor.Application.Modules;

public interface IModuleDbContextFactory<TDb>
    where TDb : IModuleDbContext
{
    Task<TDb> CreateDbContextAsync(CancellationToken ct = default);
}
