namespace EnglishLiveTutor.Application.Common.Persistence;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
