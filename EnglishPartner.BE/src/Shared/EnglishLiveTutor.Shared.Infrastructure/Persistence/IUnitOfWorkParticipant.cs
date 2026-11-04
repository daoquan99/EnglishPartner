using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Persistence;

public interface IUnitOfWorkParticipant
{
    DbContext DbContext { get; }
}

public sealed class UnitOfWorkParticipant<TContext>(TContext context) : IUnitOfWorkParticipant
    where TContext : DbContext
{
    public DbContext DbContext => context;
}
