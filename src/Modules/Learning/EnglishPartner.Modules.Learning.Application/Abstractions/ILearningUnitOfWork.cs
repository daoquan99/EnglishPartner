namespace EnglishPartner.Modules.Learning.Application.Abstractions;

public interface ILearningUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
