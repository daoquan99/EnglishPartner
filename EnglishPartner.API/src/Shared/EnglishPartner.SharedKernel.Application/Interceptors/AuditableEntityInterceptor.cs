using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using EnglishPartner.SharedKernel.Domain;
using EnglishPartner.SharedKernel.Interfaces;

namespace EnglishPartner.SharedKernel.Interceptors;

/// <summary>
/// EF Core interceptor that auto-populates audit fields on Entity
/// and handles soft-delete conversion for AggregateRoot.
/// </summary>
public sealed class AuditableEntityInterceptor(ICurrentUserService currentUser, TimeProvider timeProvider)
    : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var entry in eventData.Context.ChangeTracker.Entries<Entity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = utcNow;
                    entry.Entity.CreatedBy = currentUser.UserId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = utcNow;
                    entry.Entity.UpdatedBy = currentUser.UserId;
                    break;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
