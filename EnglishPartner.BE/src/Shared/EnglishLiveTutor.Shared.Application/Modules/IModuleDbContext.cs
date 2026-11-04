namespace EnglishLiveTutor.Application.Modules;

public interface IModuleDbContext : IAsyncDisposable, IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
