namespace EnglishPartner.Modules.AI.Application.Abstractions;

public interface IAiUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
