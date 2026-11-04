using EnglishLiveTutor.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishLiveTutor.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    // Invariant: every scoped context shares one DbConnection. Do not execute operations on these
    // contexts concurrently. A context not enlisted by the current multi-context transaction must
    // not issue commands while that transaction is open.
    private readonly IReadOnlyList<DbContext> _contexts;

    [ActivatorUtilitiesConstructor]
    public UnitOfWork(IEnumerable<IUnitOfWorkParticipant> participants)
    {
        ArgumentNullException.ThrowIfNull(participants);
        var contexts = participants.Select(p => p.DbContext).ToArray();

        // Ensure SharedDbContext comes first, then modules in registration order.
        var sharedDbIndex = Array.FindIndex(contexts, c => c.GetType().Name == "SharedDbContext");
        if (sharedDbIndex > 0)
        {
            var sharedDb = contexts[sharedDbIndex];
            var ordered = new DbContext[contexts.Length];
            ordered[0] = sharedDb;
            int dest = 1;
            for (int i = 0; i < contexts.Length; i++)
            {
                if (i != sharedDbIndex)
                {
                    ordered[dest++] = contexts[i];
                }
            }
            contexts = ordered;
        }

        ValidateContexts(contexts);
        _contexts = contexts;
    }

    public UnitOfWork(DbContext db)
        : this([db])
    {
    }

    public UnitOfWork(params DbContext[] contexts)
    {
        ArgumentNullException.ThrowIfNull(contexts);
        ValidateContexts(contexts);
        _contexts = contexts;
    }

    private static void ValidateContexts(IReadOnlyList<DbContext> contexts)
    {
        if (contexts.Count == 0 || contexts.Any(context => context is null))
        {
            throw new ArgumentException("At least one DbContext is required.", nameof(contexts));
        }
    }

    public async Task<int> CommitChangesAsync(CancellationToken ct = default)
    {
        var changedContexts = _contexts
            .Where(context => context.ChangeTracker.HasChanges())
            .ToArray();

        if (changedContexts.Length == 0)
        {
            return 0;
        }

        // Already inside ExecuteInTransactionAsync (TransactionBehavior): every context is enlisted
        // in the ambient transaction, so opening a second one on the shared connection would fail.
        if (changedContexts.Length == 1
            || !changedContexts.All(IsRelational)
            || changedContexts.Any(context => context.Database.CurrentTransaction is not null))
        {
            return await SaveContextsAsync(changedContexts, ct);
        }

        var strategy = changedContexts[0].Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await changedContexts[0].Database.BeginTransactionAsync(ct);
            try
            {
                var relationalTransaction = transaction.GetDbTransaction();
                for (int i = 1; i < changedContexts.Length; i++)
                {
                    await changedContexts[i].Database.UseTransactionAsync(relationalTransaction, ct);
                }

                int affectedRows = await SaveContextsAsync(changedContexts, ct);
                await transaction.CommitAsync(ct);
                return affectedRows;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
            finally
            {
                // Detach the completed transaction so later work in the same scope does not reuse it.
                await DetachTransactionAsync(changedContexts, ct);
            }
        });
    }

    public async Task<TResponse> ExecuteInTransactionAsync<TResponse>(
        Func<CancellationToken, Task<TResponse>> operation,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        // In-memory and other non-relational providers do not support relational transactions.
        if (!_contexts.All(IsRelational))
        {
            var response = await operation(ct);
            await SaveContextsAsync(_contexts, ct);
            return response;
        }

        var strategy = _contexts[0].Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _contexts[0].Database.BeginTransactionAsync(ct);
            try
            {
                var relationalTransaction = transaction.GetDbTransaction();
                for (int i = 1; i < _contexts.Count; i++)
                {
                    await _contexts[i].Database.UseTransactionAsync(relationalTransaction, ct);
                }

                var response = await operation(ct);
                await SaveContextsAsync(_contexts, ct);
                await transaction.CommitAsync(ct);
                return response;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
            finally
            {
                await DetachTransactionAsync(_contexts, ct);
            }
        });
    }

    // Contexts after the first joined the transaction through UseTransaction; the first context owns it
    // and EF clears it when the IDbContextTransaction is disposed.
    private static async Task DetachTransactionAsync(IReadOnlyList<DbContext> contexts, CancellationToken ct)
    {
        for (int i = 1; i < contexts.Count; i++)
        {
            if (contexts[i].Database.CurrentTransaction is not null)
            {
                await contexts[i].Database.UseTransactionAsync(null, ct);
            }
        }
    }

    private static bool IsRelational(DbContext context) =>
        context.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory" &&
        context.Database.IsRelational();

    private static async Task<int> SaveContextsAsync(
        IReadOnlyList<DbContext> contexts,
        CancellationToken ct)
    {
        int affectedRows = 0;
        foreach (var context in contexts)
        {
            affectedRows += await context.SaveChangesAsync(ct);
        }

        return affectedRows;
    }
}
