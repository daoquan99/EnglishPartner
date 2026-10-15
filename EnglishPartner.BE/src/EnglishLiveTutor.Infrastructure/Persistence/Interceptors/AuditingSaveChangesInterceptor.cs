using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EnglishLiveTutor.Infrastructure.Persistence.Interceptors;

public sealed class AuditingSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly TimeProvider _timeProvider;
    private readonly ICurrentUser _currentUser;

    public AuditingSaveChangesInterceptor(
        TimeProvider? timeProvider = null,
        ICurrentUser? currentUser = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _currentUser = currentUser ?? SystemCurrentUser.Instance;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAuditAndSoftDeleteRules(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditAndSoftDeleteRules(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditAndSoftDeleteRules(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        string actor = string.IsNullOrWhiteSpace(_currentUser.UserId)
            ? "system"
            : _currentUser.UserId!.Trim();

        // 1. Intercept unintended hard deletes on Aggregate Roots (ISoftDeletable) and convert to SoftDelete
        var softDeletedRoots = new List<EntityEntry>();
        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>().ToList())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.SoftDelete(utcNow, actor);
                entry.Property(nameof(ISoftDeletable.IsDeleted)).IsModified = true;
                entry.Property(nameof(ISoftDeletable.DeletedAtUtc)).IsModified = true;
                entry.Property(nameof(ISoftDeletable.DeletedBy)).IsModified = true;
                softDeletedRoots.Add(entry);
            }
        }

        // Preserve child entities when their parent Aggregate Root was soft-deleted via DbContext.Remove
        if (softDeletedRoots.Count > 0)
        {
            foreach (var childEntry in context.ChangeTracker.Entries().ToList())
            {
                if (childEntry.State == EntityState.Deleted && childEntry.Entity is not ISoftDeletable)
                {
                    childEntry.State = EntityState.Unchanged;
                }
            }
        }

        // 2. Maintain IAuditable properties on Added and Modified entities
        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                var createdProp = entry.Property(nameof(IAuditable.CreatedAtUtc));
                createdProp.CurrentValue = utcNow;

                var createdByProp = entry.Property(nameof(IAuditable.CreatedBy));
                if (string.IsNullOrWhiteSpace(createdByProp.CurrentValue?.ToString()))
                {
                    createdByProp.CurrentValue = actor;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                // Never overwrite creation audit values during updates
                entry.Property(nameof(IAuditable.CreatedAtUtc)).IsModified = false;
                entry.Property(nameof(IAuditable.CreatedBy)).IsModified = false;

                var updatedProp = entry.Property(nameof(IAuditable.UpdatedAtUtc));
                updatedProp.CurrentValue = utcNow;
                updatedProp.IsModified = true;

                var updatedByProp = entry.Property(nameof(IAuditable.UpdatedBy));
                updatedByProp.CurrentValue = actor;
                updatedByProp.IsModified = true;
            }
        }
    }
}

public sealed class SystemCurrentUser : ICurrentUser
{
    public static readonly SystemCurrentUser Instance = new("system");

    public string? UserId { get; }

    public SystemCurrentUser(string? userId = "system")
    {
        UserId = string.IsNullOrWhiteSpace(userId) ? "system" : userId.Trim();
    }
}
